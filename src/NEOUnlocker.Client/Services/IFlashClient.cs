namespace NEOUnlocker.Client.Services;

/// <summary>
/// Main flash client orchestration service.
/// </summary>
public interface IFlashClient
{
    /// <summary>
    /// Initiates a flash session for a device.
    /// </summary>
    /// <param name="deviceType">Device type to flash.</param>
    /// <param name="progress">Progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if flash succeeded.</returns>
    Task<bool> FlashDeviceAsync(
        string deviceType,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
