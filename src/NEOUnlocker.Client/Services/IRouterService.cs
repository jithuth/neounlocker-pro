using NEOUnlocker.Client.Models;

namespace NEOUnlocker.Client.Services;

/// <summary>
/// Interface for orchestrating the three-step router unlock process.
/// </summary>
public interface IRouterService
{
    /// <summary>
    /// Executes Step 1: Read router properties via AT commands.
    /// </summary>
    Task<RouterInfo> ExecuteStep1_ReadPropertiesAsync(string comPort, IProgress<UnlockProgress>? progress = null);

    /// <summary>
    /// Executes Step 2: Switch device to fastboot mode and detect.
    /// </summary>
    Task<bool> ExecuteStep2_SwitchToFastbootAsync(IProgress<UnlockProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes Step 3: Flash bootloader and unlock device.
    /// </summary>
    Task<bool> ExecuteStep3_UnlockAsync(IProgress<UnlockProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the router information from Step 1.
    /// </summary>
    RouterInfo? CurrentRouterInfo { get; }

    /// <summary>
    /// Gets the current step (0 = not started, 1-3 = in progress).
    /// </summary>
    int CurrentStep { get; }
}
