namespace NEOUnlocker.Client.Models;

/// <summary>
/// Represents progress information for the unlock operation.
/// </summary>
public class UnlockProgress
{
    /// <summary>
    /// Current step (1, 2, or 3).
    /// </summary>
    public int Step { get; set; }

    /// <summary>
    /// Current stage within the step (e.g., "Reading IMEI", "Flashing").
    /// </summary>
    public string Stage { get; set; } = string.Empty;

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int Percentage { get; set; }

    /// <summary>
    /// Detailed progress message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Number of bytes transferred (optional).
    /// </summary>
    public long? BytesTransferred { get; set; }

    /// <summary>
    /// Total bytes to transfer (optional).
    /// </summary>
    public long? TotalBytes { get; set; }

    /// <summary>
    /// Transfer speed in MB/s (optional).
    /// </summary>
    public double? SpeedMBps { get; set; }
}
