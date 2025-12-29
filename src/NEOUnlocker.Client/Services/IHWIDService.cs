namespace NEOUnlocker.Client.Services;

/// <summary>
/// Service for generating hardware ID.
/// </summary>
public interface IHWIDService
{
    /// <summary>
    /// Gets the hardware ID of the current device.
    /// </summary>
    /// <returns>Hardware ID string.</returns>
    string GetHardwareId();
}
