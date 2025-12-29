using System.IO;

namespace NEOUnlocker.Client.Services;

/// <summary>
/// Service for executing native flash tools securely.
/// </summary>
public interface INativeToolExecutor
{
    /// <summary>
    /// Executes a native tool with firmware data.
    /// </summary>
    /// <param name="toolName">Name of the tool executable (e.g., bln.exe, fastboot.exe).</param>
    /// <param name="arguments">Command-line arguments.</param>
    /// <param name="firmwareStreams">Dictionary of firmware file names to decrypted streams.</param>
    /// <param name="progress">Progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if execution succeeded.</returns>
    Task<bool> ExecuteToolAsync(
        string toolName,
        string arguments,
        Dictionary<string, Stream> firmwareStreams,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
