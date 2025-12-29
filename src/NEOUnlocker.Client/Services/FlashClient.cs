using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NEOUnlocker.Client.Services;

/// <summary>
/// Implementation of flash client orchestration.
/// </summary>
public class FlashClient : IFlashClient
{
    private readonly HttpClient _httpClient;
    private readonly IKeyManagementService _keyManagement;
    private readonly IHWIDService _hwidService;
    private readonly INativeToolExecutor _toolExecutor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FlashClient> _logger;

    // Device type to tool mappings
    private static readonly Dictionary<string, (string Tool, string Args)> DeviceToolMap = new()
    {
        ["MTK6580"] = ("bln.exe", "-flash {system.bin} -loader {usbloader-5577.bin}"),
        ["Qualcomm8937"] = ("fastboot.exe", "flash system {system.bin}") // Note: boot.img flashing would be separate command
    };

    public FlashClient(
        HttpClient httpClient,
        IKeyManagementService keyManagement,
        IHWIDService hwidService,
        INativeToolExecutor toolExecutor,
        IConfiguration configuration,
        ILogger<FlashClient> logger)
    {
        _httpClient = httpClient;
        _keyManagement = keyManagement;
        _hwidService = hwidService;
        _toolExecutor = toolExecutor;
        _configuration = configuration;
        _logger = logger;
        
        // Configure HttpClient
        var baseUrl = configuration["ServerSettings:BaseUrl"] ?? "https://localhost:5001";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
    }

    /// <inheritdoc/>
    public async Task<bool> FlashDeviceAsync(
        string deviceType,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Starting flash process...");
        
        if (!DeviceToolMap.TryGetValue(deviceType, out var toolInfo))
        {
            _logger.LogError("Unknown device type: {DeviceType}", deviceType);
            progress?.Report($"Error: Unknown device type: {deviceType}");
            return false;
        }

        string? sessionId = null;
        byte[]? sessionKey = null;
        
        try
        {
            // Step 1: Create flash session
            progress?.Report("Creating flash session...");
            var sessionResponse = await CreateSessionAsync(deviceType, cancellationToken);
            
            if (sessionResponse == null)
            {
                progress?.Report("Error: Failed to create session");
                return false;
            }
            
            sessionId = sessionResponse.SessionId;
            
            progress?.Report($"Session created: {sessionId}");
            progress?.Report($"Session expires at: {sessionResponse.ExpiresAt}");
            
            // Step 2: Unwrap session key
            progress?.Report("Unwrapping session key...");
            var wrappedKey = Convert.FromBase64String(sessionResponse.WrappedSessionKeyBase64);
            sessionKey = _keyManagement.UnwrapSessionKey(wrappedKey);
            
            progress?.Report("Session key unwrapped successfully");
            
            // Step 3: Download and decrypt firmware
            progress?.Report("Downloading firmware files...");
            var firmwareStreams = await DownloadAndDecryptFirmwareAsync(
                sessionId,
                sessionResponse.FirmwareFiles,
                sessionKey,
                progress,
                cancellationToken);
            
            if (firmwareStreams == null || firmwareStreams.Count == 0)
            {
                progress?.Report("Error: Failed to download firmware");
                await CompleteSessionAsync(sessionId, false, "Failed to download firmware");
                return false;
            }
            
            progress?.Report($"Downloaded {firmwareStreams.Count} firmware files");
            
            // Step 4: Execute native tool
            progress?.Report($"Executing {toolInfo.Tool}...");
            var flashSuccess = await _toolExecutor.ExecuteToolAsync(
                toolInfo.Tool,
                toolInfo.Args,
                firmwareStreams,
                progress,
                cancellationToken);
            
            // Step 5: Complete session
            progress?.Report("Completing session...");
            await CompleteSessionAsync(sessionId, flashSuccess, flashSuccess ? null : "Flash tool failed");
            
            if (flashSuccess)
            {
                progress?.Report("Flash completed successfully!");
                return true;
            }
            else
            {
                progress?.Report("Flash failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Flash operation failed");
            progress?.Report($"Error: {ex.Message}");
            
            if (sessionId != null)
            {
                try
                {
                    await CompleteSessionAsync(sessionId, false, ex.Message);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
            
            return false;
        }
        finally
        {
            // Zero session key
            if (sessionKey != null)
            {
                CryptographicOperations.ZeroMemory(sessionKey);
            }
        }
    }

    private async Task<FlashSessionResponse?> CreateSessionAsync(
        string deviceType,
        CancellationToken cancellationToken)
    {
        try
        {
            var hwid = _hwidService.GetHardwareId();
            var publicKeyPem = _keyManagement.ExportPublicKeyPem();
            
            var request = new
            {
                HWID = hwid,
                DeviceType = deviceType,
                ClientPublicKeyPem = publicKeyPem
            };
            
            var response = await _httpClient.PostAsJsonAsync(
                "/api/flash/sessions",
                request,
                cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to create session: {StatusCode} - {Error}", 
                    response.StatusCode, error);
                return null;
            }
            
            return await response.Content.ReadFromJsonAsync<FlashSessionResponse>(
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session");
            return null;
        }
    }

    private async Task<Dictionary<string, Stream>?> DownloadAndDecryptFirmwareAsync(
        string sessionId,
        List<string> firmwareFiles,
        byte[] sessionKey,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var streams = new Dictionary<string, Stream>();
        var hwid = _hwidService.GetHardwareId();
        
        try
        {
            foreach (var fileName in firmwareFiles)
            {
                progress?.Report($"Downloading {fileName}...");
                
                var url = $"/api/flash/sessions/{sessionId}/firmware/{fileName}?hwid={hwid}";
                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to download {FileName}: {StatusCode}", 
                        fileName, response.StatusCode);
                    return null;
                }
                
                // Read encrypted firmware
                var encryptedData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                
                progress?.Report($"Decrypting {fileName}...");
                
                // Decrypt in memory
                var decryptedData = DecryptFirmware(encryptedData, sessionKey);
                
                // Create memory stream with decrypted data
                var stream = new MemoryStream(decryptedData);
                streams[fileName] = stream;
                
                // Zero encrypted data
                CryptographicOperations.ZeroMemory(encryptedData);
                
                progress?.Report($"Downloaded and decrypted {fileName} ({decryptedData.Length} bytes)");
            }
            
            return streams;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download firmware");
            
            // Clean up any streams created
            foreach (var stream in streams.Values)
            {
                stream.Dispose();
            }
            
            return null;
        }
    }

    private byte[] DecryptFirmware(byte[] encryptedData, byte[] sessionKey)
    {
        // Format: [12-byte nonce][16-byte tag][encrypted data]
        if (encryptedData.Length < 28)
        {
            throw new CryptographicException("Invalid encrypted data format");
        }

        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var ciphertext = new byte[encryptedData.Length - 28];
        
        Buffer.BlockCopy(encryptedData, 0, nonce, 0, 12);
        Buffer.BlockCopy(encryptedData, 12, tag, 0, 16);
        Buffer.BlockCopy(encryptedData, 28, ciphertext, 0, ciphertext.Length);

        var plaintext = new byte[ciphertext.Length];
        
        using var aesGcm = new AesGcm(sessionKey, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        
        // Zero temporary buffers
        CryptographicOperations.ZeroMemory(nonce);
        CryptographicOperations.ZeroMemory(tag);
        CryptographicOperations.ZeroMemory(ciphertext);
        
        return plaintext;
    }

    private async Task CompleteSessionAsync(string sessionId, bool success, string? errorMessage)
    {
        try
        {
            var hwid = _hwidService.GetHardwareId();
            
            var request = new
            {
                HWID = hwid,
                Success = success,
                ErrorMessage = errorMessage
            };
            
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/flash/sessions/{sessionId}/complete",
                request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to complete session: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete session");
        }
    }

    private class FlashSessionResponse
    {
        public required string SessionId { get; init; }
        public required string WrappedSessionKeyBase64 { get; init; }
        public required string ExpiresAt { get; init; }
        public required string Status { get; init; }
        public required List<string> FirmwareFiles { get; init; }
        public int CreditCost { get; init; }
    }
}
