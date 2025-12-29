namespace NEOUnlocker.Client.Models;

/// <summary>
/// Represents the result of an unlock operation.
/// </summary>
public class UnlockResult
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Router information retrieved during the operation.
    /// </summary>
    public RouterInfo? RouterInfo { get; set; }

    /// <summary>
    /// Duration of the operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Operation logs.
    /// </summary>
    public List<string> Logs { get; set; } = new();
}
