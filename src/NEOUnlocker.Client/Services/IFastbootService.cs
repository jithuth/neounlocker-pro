using NEOUnlocker.Client.Models;

namespace NEOUnlocker.Client.Services;

/// <summary>
/// Interface for fastboot device operations.
/// </summary>
public interface IFastbootService
{
    /// <summary>
    /// Detects if a fastboot device is connected.
    /// </summary>
    Task<bool> DetectDeviceAsync();

    /// <summary>
    /// Gets the serial number of the connected fastboot device.
    /// </summary>
    Task<string?> GetDeviceSerialAsync();

    /// <summary>
    /// Flashes a bootloader file to the device.
    /// </summary>
    Task<bool> FlashBootloaderAsync(string bootloaderPath, IProgress<UnlockProgress>? progress = null);

    /// <summary>
    /// Unlocks the device with an optional unlock code.
    /// </summary>
    Task<bool> UnlockDeviceAsync(string? unlockCode = null);

    /// <summary>
    /// Reboots the device.
    /// </summary>
    Task<bool> RebootDeviceAsync();
}
