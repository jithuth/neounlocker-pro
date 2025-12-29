namespace NEOUnlocker.Server.Services;

/// <summary>
/// Service for managing encrypted firmware files.
/// </summary>
public interface IFirmwareService
{
    /// <summary>
    /// Gets the list of firmware files required for a specific device type.
    /// </summary>
    /// <param name="deviceType">Device type (e.g., MTK6580, Qualcomm8937).</param>
    /// <returns>List of firmware file names.</returns>
    List<string> GetRequiredFirmwareFiles(string deviceType);
    
    /// <summary>
    /// Checks if all required firmware files exist for a device type.
    /// </summary>
    /// <param name="deviceType">Device type to check.</param>
    /// <returns>True if all files exist, false otherwise.</returns>
    bool AreAllFirmwareFilesAvailable(string deviceType);
    
    /// <summary>
    /// Streams encrypted firmware file re-encrypted with the session key.
    /// </summary>
    /// <param name="fileName">Name of the firmware file.</param>
    /// <param name="sessionKey">Session key for re-encryption.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream of re-encrypted firmware data.</returns>
    Task<Stream> GetReEncryptedFirmwareStreamAsync(
        string fileName,
        byte[] sessionKey,
        CancellationToken cancellationToken = default);
}
