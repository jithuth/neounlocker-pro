using NEOUnlocker.Server.Models;

namespace NEOUnlocker.Server.Services;

/// <summary>
/// Service for managing flash session lifecycle.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Creates a new flash session.
    /// </summary>
    /// <param name="hwid">Hardware ID of the client.</param>
    /// <param name="deviceType">Device type to flash.</param>
    /// <param name="clientPublicKeyPem">Client's RSA public key in PEM format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created flash session.</returns>
    Task<FlashSession> CreateSessionAsync(
        string hwid,
        string deviceType,
        string clientPublicKeyPem,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a flash session by ID.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="hwid">Hardware ID for validation.</param>
    /// <returns>The flash session, or null if not found or HWID mismatch.</returns>
    FlashSession? GetSession(string sessionId, string hwid);
    
    /// <summary>
    /// Validates that a session is usable.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="hwid">Hardware ID for validation.</param>
    /// <returns>The session if valid and usable.</returns>
    /// <exception cref="InvalidOperationException">If session is not usable.</exception>
    FlashSession ValidateSession(string sessionId, string hwid);
    
    /// <summary>
    /// Marks a session as completed or failed and burns it.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="hwid">Hardware ID for validation.</param>
    /// <param name="success">Whether the flash succeeded.</param>
    /// <param name="errorMessage">Optional error message.</param>
    /// <returns>True if session was burned successfully.</returns>
    bool CompleteSession(string sessionId, string hwid, bool success, string? errorMessage = null);
    
    /// <summary>
    /// Cleans up expired sessions.
    /// </summary>
    void CleanupExpiredSessions();
}
