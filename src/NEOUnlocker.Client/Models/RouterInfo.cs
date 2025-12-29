namespace NEOUnlocker.Client.Models;

/// <summary>
/// Contains information about the router retrieved via AT commands.
/// </summary>
public class RouterInfo
{
    /// <summary>
    /// Manufacturer name (from AT+CGMI).
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Model name (from AT+CGMM).
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// IMEI/Serial Number (from AT+CGSN).
    /// </summary>
    public string? IMEI { get; set; }

    /// <summary>
    /// Firmware version (from AT+CGMR).
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Lock status (from AT^CARDLOCK?).
    /// </summary>
    public string? LockStatus { get; set; }

    /// <summary>
    /// Device information (from ATI).
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Timestamp when the information was retrieved.
    /// </summary>
    public DateTime ReadTimestamp { get; set; }

    /// <summary>
    /// Returns a string representation of the router information.
    /// </summary>
    public override string ToString()
    {
        return $"Model: {Model}, IMEI: {IMEI}, Firmware: {FirmwareVersion}";
    }
}
