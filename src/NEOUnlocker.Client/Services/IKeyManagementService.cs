namespace NEOUnlocker.Client.Services;

/// <summary>
/// Service for managing RSA keypair with Windows DPAPI protection.
/// </summary>
public interface IKeyManagementService
{
    /// <summary>
    /// Gets or generates the client's RSA keypair.
    /// </summary>
    /// <returns>RSA instance with the client's keypair.</returns>
    System.Security.Cryptography.RSA GetOrCreateKeyPair();
    
    /// <summary>
    /// Exports the public key in PEM format.
    /// </summary>
    /// <returns>PEM-encoded public key.</returns>
    string ExportPublicKeyPem();
    
    /// <summary>
    /// Unwraps (decrypts) a session key using the private key.
    /// </summary>
    /// <param name="wrappedKey">Wrapped session key.</param>
    /// <returns>Unwrapped session key.</returns>
    byte[] UnwrapSessionKey(byte[] wrappedKey);
}
