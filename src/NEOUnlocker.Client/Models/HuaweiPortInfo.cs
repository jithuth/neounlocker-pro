namespace NEOUnlocker.Client.Models;

/// <summary>
/// Information about a detected Huawei COM port.
/// </summary>
public class HuaweiPortInfo
{
    /// <summary>
    /// Gets or sets the COM port name (e.g., "COM5").
    /// </summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port type (e.g., "3G_PCUI", "Download", "Modem").
    /// </summary>
    public string PortType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full device description from Windows.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Huawei Vendor ID (should be "12D1").
    /// </summary>
    public string VID { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Product ID.
    /// </summary>
    public string PID { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the port is currently available (not in use by another application).
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Gets the device mode based on available port types.
    /// </summary>
    public DeviceMode DeviceMode => PortType switch
    {
        "3G_PCUI" => DeviceMode.ModemMode,
        "Modem" => DeviceMode.ModemMode,
        "Download" => DeviceMode.FastbootMode,
        _ => DeviceMode.Unknown
    };

    /// <summary>
    /// Gets a user-friendly display name for the port.
    /// </summary>
    public string DisplayName => $"{PortName} - {PortType}" + (IsAvailable ? "" : " (In Use)");

    /// <summary>
    /// Gets the recommended baud rate for this port type.
    /// </summary>
    public int RecommendedBaudRate => PortType switch
    {
        "3G_PCUI" => 115200,
        "Modem" => 115200,
        "Download" => 115200,
        _ => 115200
    };
}

/// <summary>
/// Represents the current mode of a Huawei device.
/// </summary>
public enum DeviceMode
{
    /// <summary>
    /// Device mode is unknown or cannot be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// Device is in modem mode (3G PC UI Interface available).
    /// </summary>
    ModemMode,

    /// <summary>
    /// Device is in fastboot mode (Download port available).
    /// </summary>
    FastbootMode,

    /// <summary>
    /// Device is in ADB mode (ADB port available).
    /// </summary>
    ADBMode
}
