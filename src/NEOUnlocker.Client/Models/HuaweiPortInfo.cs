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
