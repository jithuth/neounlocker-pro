using System.Security.Cryptography;

namespace NEOUnlocker.Server.Services;

/// <summary>
/// Implementation of firmware management service.
/// Handles encrypted firmware storage and re-encryption with session keys.
/// </summary>
public class FirmwareService : IFirmwareService
{
    private readonly string _storagePath;
    private readonly byte[] _masterKey;
    private readonly ILogger<FirmwareService> _logger;
    
    // Device type to firmware file mappings
    private static readonly Dictionary<string, List<string>> DeviceFirmwareMap = new()
    {
        ["MTK6580"] = new() { "system.bin", "usbloader-5577.bin" },
        ["Qualcomm8937"] = new() { "system.bin", "boot.img" }
    };

    public FirmwareService(IConfiguration configuration, ILogger<FirmwareService> logger)
    {
        _logger = logger;
        
        var storagePath = configuration["FirmwareSettings:StoragePath"];
        if (string.IsNullOrEmpty(storagePath))
        {
            throw new InvalidOperationException("FirmwareSettings:StoragePath not configured");
        }
        
        _storagePath = storagePath;
        
        // Get master encryption key from configuration
        var masterKeyBase64 = configuration["FirmwareSettings:MasterEncryptionKeyBase64"];
        if (string.IsNullOrEmpty(masterKeyBase64))
        {
            // Generate a random key for development (NOT for production)
            _logger.LogWarning("Master encryption key not configured. Using random key (DEVELOPMENT ONLY).");
            _masterKey = new byte[32];
            RandomNumberGenerator.Fill(_masterKey);
        }
        else
        {
            try
            {
                _masterKey = Convert.FromBase64String(masterKeyBase64);
                if (_masterKey.Length != 32)
                {
                    throw new InvalidOperationException("Master key must be 32 bytes (256 bits)");
                }
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Invalid master key format. Must be Base64 encoded.");
            }
        }
        
        // Ensure storage directory exists
        Directory.CreateDirectory(_storagePath);
        
        _logger.LogInformation("FirmwareService initialized with storage path: {StoragePath}", _storagePath);
    }

    /// <inheritdoc/>
    public List<string> GetRequiredFirmwareFiles(string deviceType)
    {
        if (DeviceFirmwareMap.TryGetValue(deviceType, out var files))
        {
            return new List<string>(files);
        }
        
        _logger.LogWarning("Unknown device type requested: {DeviceType}", deviceType);
        throw new ArgumentException($"Unknown device type: {deviceType}", nameof(deviceType));
    }

    /// <inheritdoc/>
    public bool AreAllFirmwareFilesAvailable(string deviceType)
    {
        var requiredFiles = GetRequiredFirmwareFiles(deviceType);
        
        foreach (var fileName in requiredFiles)
        {
            var encryptedPath = GetEncryptedFilePath(fileName);
            if (!File.Exists(encryptedPath))
            {
                _logger.LogWarning("Missing firmware file: {FileName}", fileName);
                return false;
            }
        }
        
        return true;
    }

    /// <inheritdoc/>
    public async Task<Stream> GetReEncryptedFirmwareStreamAsync(
        string fileName,
        byte[] sessionKey,
        CancellationToken cancellationToken = default)
    {
        if (sessionKey.Length != 32)
        {
            throw new ArgumentException("Session key must be 32 bytes", nameof(sessionKey));
        }

        var encryptedPath = GetEncryptedFilePath(fileName);
        
        if (!File.Exists(encryptedPath))
        {
            _logger.LogError("Firmware file not found: {FileName}", fileName);
            throw new FileNotFoundException($"Firmware file not found: {fileName}");
        }

        _logger.LogInformation("Re-encrypting firmware file: {FileName}", fileName);

        // Read encrypted firmware
        var encryptedData = await File.ReadAllBytesAsync(encryptedPath, cancellationToken);
        
        byte[]? decryptedData = null;
        byte[]? reEncryptedData = null;
        
        try
        {
            // Decrypt with master key
            decryptedData = DecryptWithMasterKey(encryptedData);
            
            // Re-encrypt with session key
            reEncryptedData = EncryptWithSessionKey(decryptedData, sessionKey);
            
            // Return as memory stream
            var stream = new MemoryStream(reEncryptedData);
            
            _logger.LogInformation("Successfully re-encrypted firmware file: {FileName} ({Size} bytes)", 
                fileName, reEncryptedData.Length);
            
            return stream;
        }
        finally
        {
            // Zero sensitive data
            if (decryptedData != null)
            {
                CryptographicOperations.ZeroMemory(decryptedData);
            }
            // Note: reEncryptedData is returned in stream, will be zeroed by caller
        }
    }

    private string GetEncryptedFilePath(string fileName)
    {
        // Store encrypted files with .enc extension
        return Path.Combine(_storagePath, $"{fileName}.enc");
    }

    private byte[] DecryptWithMasterKey(byte[] encryptedData)
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
        
        using var aesGcm = new AesGcm(_masterKey, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        
        // Zero temporary buffers
        CryptographicOperations.ZeroMemory(nonce);
        CryptographicOperations.ZeroMemory(tag);
        CryptographicOperations.ZeroMemory(ciphertext);
        
        return plaintext;
    }

    private byte[] EncryptWithSessionKey(byte[] plaintext, byte[] sessionKey)
    {
        // Generate random nonce
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);
        
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        
        using var aesGcm = new AesGcm(sessionKey, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        
        // Format: [12-byte nonce][16-byte tag][encrypted data]
        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);
        
        // Zero temporary buffers
        CryptographicOperations.ZeroMemory(nonce);
        CryptographicOperations.ZeroMemory(tag);
        CryptographicOperations.ZeroMemory(ciphertext);
        
        return result;
    }
}
