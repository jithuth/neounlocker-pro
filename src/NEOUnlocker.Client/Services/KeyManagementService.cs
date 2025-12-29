using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NEOUnlocker.Client.Services;

/// <summary>
/// Implementation of key management service using Windows DPAPI.
/// </summary>
public class KeyManagementService : IKeyManagementService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeyManagementService> _logger;
    private readonly string _keyStoragePath;
    private readonly int _rsaKeySize;
    private RSA? _rsa;

    public KeyManagementService(IConfiguration configuration, ILogger<KeyManagementService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        _rsaKeySize = configuration.GetValue<int>("SecuritySettings:RSAKeySize", 2048);
        
        // Store encrypted keys in user's local app data
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _keyStoragePath = Path.Combine(appData, "NEOUnlocker", "keys");
        Directory.CreateDirectory(_keyStoragePath);
        
        _logger.LogInformation("KeyManagementService initialized with {KeySize}-bit RSA", _rsaKeySize);
    }

    /// <inheritdoc/>
    public RSA GetOrCreateKeyPair()
    {
        if (_rsa != null)
        {
            return _rsa;
        }

        var keyFilePath = Path.Combine(_keyStoragePath, "client_key.dat");

        if (File.Exists(keyFilePath))
        {
            try
            {
                _logger.LogInformation("Loading existing RSA keypair from storage");
                _rsa = LoadKeyPair(keyFilePath);
                return _rsa;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load existing keypair, generating new one");
            }
        }

        _logger.LogInformation("Generating new RSA keypair");
        _rsa = RSA.Create(_rsaKeySize);
        SaveKeyPair(_rsa, keyFilePath);
        
        return _rsa;
    }

    /// <inheritdoc/>
    public string ExportPublicKeyPem()
    {
        var rsa = GetOrCreateKeyPair();
        return rsa.ExportSubjectPublicKeyInfoPem();
    }

    /// <inheritdoc/>
    public byte[] UnwrapSessionKey(byte[] wrappedKey)
    {
        var rsa = GetOrCreateKeyPair();
        
        try
        {
            // Decrypt using RSA-OAEP with SHA256
            return rsa.Decrypt(wrappedKey, RSAEncryptionPadding.OaepSHA256);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to unwrap session key");
            throw new InvalidOperationException("Failed to unwrap session key", ex);
        }
    }

    private void SaveKeyPair(RSA rsa, string filePath)
    {
        try
        {
            // Export private key as PKCS8
            var privateKey = rsa.ExportPkcs8PrivateKey();
            
            // Encrypt with DPAPI (Windows Data Protection API)
            var encryptedKey = ProtectedData.Protect(
                privateKey,
                null, // optional entropy
                DataProtectionScope.CurrentUser);
            
            // Save encrypted key
            File.WriteAllBytes(filePath, encryptedKey);
            
            // Zero the plaintext key
            CryptographicOperations.ZeroMemory(privateKey);
            
            _logger.LogInformation("Saved encrypted RSA keypair to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save keypair");
            throw;
        }
    }

    private RSA LoadKeyPair(string filePath)
    {
        try
        {
            // Read encrypted key
            var encryptedKey = File.ReadAllBytes(filePath);
            
            // Decrypt with DPAPI
            var privateKey = ProtectedData.Unprotect(
                encryptedKey,
                null, // optional entropy
                DataProtectionScope.CurrentUser);
            
            // Import into RSA
            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKey, out _);
            
            // Zero the plaintext key
            CryptographicOperations.ZeroMemory(privateKey);
            
            _logger.LogInformation("Loaded encrypted RSA keypair from {FilePath}", filePath);
            
            return rsa;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load keypair");
            throw;
        }
    }
}
