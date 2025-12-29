using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using NEOUnlocker.Server.Models;

namespace NEOUnlocker.Server.Services;

/// <summary>
/// Implementation of session management service.
/// Handles session creation, validation, and lifecycle management.
/// </summary>
public class SessionService : ISessionService
{
    private readonly ConcurrentDictionary<string, FlashSession> _sessions = new();
    private readonly IFirmwareService _firmwareService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SessionService> _logger;
    private readonly int _sessionExpiryMinutes;

    public SessionService(
        IFirmwareService firmwareService,
        IConfiguration configuration,
        ILogger<SessionService> logger)
    {
        _firmwareService = firmwareService;
        _configuration = configuration;
        _logger = logger;
        
        _sessionExpiryMinutes = configuration.GetValue<int>("SessionSettings:SessionExpiryMinutes", 15);
        
        _logger.LogInformation("SessionService initialized with {ExpiryMinutes} minute expiry", 
            _sessionExpiryMinutes);
    }

    /// <inheritdoc/>
    public async Task<FlashSession> CreateSessionAsync(
        string hwid,
        string deviceType,
        string clientPublicKeyPem,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hwid))
        {
            throw new ArgumentException("HWID cannot be empty", nameof(hwid));
        }
        
        if (string.IsNullOrWhiteSpace(deviceType))
        {
            throw new ArgumentException("Device type cannot be empty", nameof(deviceType));
        }
        
        if (string.IsNullOrWhiteSpace(clientPublicKeyPem))
        {
            throw new ArgumentException("Client public key cannot be empty", nameof(clientPublicKeyPem));
        }

        // Validate firmware is available
        if (!_firmwareService.AreAllFirmwareFilesAvailable(deviceType))
        {
            throw new InvalidOperationException($"Firmware not available for device type: {deviceType}");
        }

        // Generate session ID and key
        var sessionId = GenerateSessionId();
        var sessionKey = new byte[32]; // AES-256 key
        RandomNumberGenerator.Fill(sessionKey);

        byte[]? wrappedKey = null;
        try
        {
            // Wrap session key with client's RSA public key
            wrappedKey = WrapSessionKey(sessionKey, clientPublicKeyPem);

            var now = DateTime.UtcNow;
            var session = new FlashSession
            {
                SessionId = sessionId,
                HWID = hwid,
                DeviceType = deviceType,
                SessionKey = sessionKey,
                WrappedSessionKey = wrappedKey,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(_sessionExpiryMinutes),
                Status = FlashSessionStatus.Active,
                RequiredFirmwareFiles = _firmwareService.GetRequiredFirmwareFiles(deviceType),
                CreditCost = GetCreditCost(deviceType)
            };

            if (!_sessions.TryAdd(sessionId, session))
            {
                throw new InvalidOperationException("Failed to create session (duplicate ID)");
            }

            _logger.LogInformation(
                "Created session {SessionId} for HWID {HWID}, device {DeviceType}, expires at {ExpiresAt}",
                sessionId, hwid, deviceType, session.ExpiresAt);

            return session;
        }
        catch
        {
            // Zero sensitive data on error
            if (sessionKey != null)
            {
                CryptographicOperations.ZeroMemory(sessionKey);
            }
            throw;
        }
    }

    /// <inheritdoc/>
    public FlashSession? GetSession(string sessionId, string hwid)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            return null;
        }

        // Validate HWID
        if (session.HWID != hwid)
        {
            _logger.LogWarning(
                "HWID mismatch for session {SessionId}. Expected: {Expected}, Got: {Actual}",
                sessionId, session.HWID, hwid);
            return null;
        }

        // Check if expired and update status
        if (session.IsExpired && session.Status == FlashSessionStatus.Active)
        {
            session.Status = FlashSessionStatus.Expired;
            _logger.LogInformation("Session {SessionId} expired", sessionId);
        }

        return session;
    }

    /// <inheritdoc/>
    public FlashSession ValidateSession(string sessionId, string hwid)
    {
        var session = GetSession(sessionId, hwid);
        
        if (session == null)
        {
            throw new InvalidOperationException("Session not found or HWID mismatch");
        }

        if (!session.IsUsable)
        {
            throw new InvalidOperationException($"Session is not usable. Status: {session.Status}");
        }

        return session;
    }

    /// <inheritdoc/>
    public bool CompleteSession(string sessionId, string hwid, bool success, string? errorMessage = null)
    {
        var session = GetSession(sessionId, hwid);
        
        if (session == null)
        {
            _logger.LogWarning("Cannot complete session {SessionId}: not found or HWID mismatch", sessionId);
            return false;
        }

        // Update status
        var finalStatus = success ? FlashSessionStatus.Completed : FlashSessionStatus.Failed;
        session.Status = finalStatus;
        session.ErrorMessage = errorMessage;

        _logger.LogInformation(
            "Session {SessionId} completed. Success: {Success}, Status: {Status}",
            sessionId, success, finalStatus);

        // In production, this would deduct credits from user account
        if (success)
        {
            _logger.LogInformation(
                "Would deduct {Credits} credits for session {SessionId}",
                session.CreditCost, sessionId);
        }

        // Burn the session (mark as burned after recording the final status)
        session.Status = FlashSessionStatus.Burned;

        // Zero the session key
        CryptographicOperations.ZeroMemory(session.SessionKey);

        return true;
    }

    /// <inheritdoc/>
    public void CleanupExpiredSessions()
    {
        var expiredSessions = _sessions
            .Where(kvp => kvp.Value.IsExpired || kvp.Value.Status == FlashSessionStatus.Burned)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var sessionId in expiredSessions)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                // Zero session key before removal
                CryptographicOperations.ZeroMemory(session.SessionKey);
                _logger.LogDebug("Cleaned up session {SessionId}", sessionId);
            }
        }

        if (expiredSessions.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired/burned sessions", expiredSessions.Count);
        }
    }

    private string GenerateSessionId()
    {
        // Generate cryptographically secure random session ID
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private byte[] WrapSessionKey(byte[] sessionKey, string clientPublicKeyPem)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(clientPublicKeyPem);
            
            // Use RSA-OAEP with SHA256
            return rsa.Encrypt(sessionKey, RSAEncryptionPadding.OaepSHA256);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to wrap session key with client public key");
            throw new CryptographicException("Failed to wrap session key", ex);
        }
    }

    private int GetCreditCost(string deviceType)
    {
        // Different device types may have different costs
        return deviceType switch
        {
            "MTK6580" => 1,
            "Qualcomm8937" => 1,
            _ => 1
        };
    }
}
