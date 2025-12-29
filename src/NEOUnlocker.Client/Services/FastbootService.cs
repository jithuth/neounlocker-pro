using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NEOUnlocker.Client.Models;

namespace NEOUnlocker.Client.Services;

/// <summary>
/// Service for executing fastboot operations.
/// </summary>
public class FastbootService : IFastbootService
{
    private readonly ILogger<FastbootService> _logger;
    private readonly string _fastbootPath;
    private static readonly Regex DeviceSerialRegex = new(@"^([A-Z0-9]+)\s+fastboot", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private const string FastbootSendingKeyword = "sending";

    public FastbootService(ILogger<FastbootService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _fastbootPath = configuration["Fastboot:ExecutablePath"] ?? "./Tools/fastboot.exe";

        // Resolve relative path
        if (!Path.IsPathRooted(_fastbootPath))
        {
            _fastbootPath = Path.Combine(AppContext.BaseDirectory, _fastbootPath);
        }
    }

    public async Task<bool> DetectDeviceAsync()
    {
        try
        {
            _logger.LogInformation("Detecting fastboot device...");
            var result = await ExecuteFastbootCommandAsync("devices");

            if (result.Success && !string.IsNullOrWhiteSpace(result.Output))
            {
                // Check if output contains a device serial number
                var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var hasDevice = lines.Any(line =>
                    line.Contains("fastboot", StringComparison.OrdinalIgnoreCase) ||
                    line.Trim().Length > 5); // Device serial is usually longer

                _logger.LogInformation("Fastboot device detection: {Result}", hasDevice);
                return hasDevice;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect fastboot device");
            return false;
        }
    }

    public async Task<string?> GetDeviceSerialAsync()
    {
        try
        {
            var result = await ExecuteFastbootCommandAsync("devices");

            if (result.Success && !string.IsNullOrWhiteSpace(result.Output))
            {
                // Parse output: "serial_number    fastboot"
                var match = DeviceSerialRegex.Match(result.Output);
                if (match.Success)
                {
                    var serial = match.Groups[1].Value;
                    _logger.LogInformation("Found fastboot device: {Serial}", serial);
                    return serial;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device serial");
            return null;
        }
    }

    public async Task<bool> FlashBootloaderAsync(string bootloaderPath, IProgress<UnlockProgress>? progress = null)
    {
        if (!File.Exists(bootloaderPath))
        {
            _logger.LogError("Bootloader file not found: {Path}", bootloaderPath);
            return false;
        }

        try
        {
            _logger.LogInformation("Flashing bootloader: {Path}", bootloaderPath);

            progress?.Report(new UnlockProgress
            {
                Step = 3,
                Stage = "Flashing",
                Percentage = 0,
                Message = "Starting bootloader flash..."
            });

            var result = await ExecuteFastbootCommandAsync($"flash bootloader \"{bootloaderPath}\"", progress);

            if (result.Success)
            {
                _logger.LogInformation("Bootloader flashed successfully");
                progress?.Report(new UnlockProgress
                {
                    Step = 3,
                    Stage = "Flashing",
                    Percentage = 100,
                    Message = "Bootloader flashed successfully"
                });
                return true;
            }
            else
            {
                _logger.LogError("Failed to flash bootloader: {Error}", result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during bootloader flash");
            return false;
        }
    }

    public async Task<bool> UnlockDeviceAsync(string? unlockCode = null)
    {
        try
        {
            _logger.LogInformation("Unlocking device...");

            var command = string.IsNullOrWhiteSpace(unlockCode)
                ? "oem unlock"
                : $"oem unlock {unlockCode}";

            var result = await ExecuteFastbootCommandAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("Device unlocked successfully");
                return true;
            }
            else
            {
                _logger.LogError("Failed to unlock device: {Error}", result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during device unlock");
            return false;
        }
    }

    public async Task<bool> RebootDeviceAsync()
    {
        try
        {
            _logger.LogInformation("Rebooting device...");
            var result = await ExecuteFastbootCommandAsync("reboot");
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reboot device");
            return false;
        }
    }

    private async Task<(bool Success, string Output, string Error)> ExecuteFastbootCommandAsync(
        string arguments,
        IProgress<UnlockProgress>? progress = null)
    {
        if (!File.Exists(_fastbootPath))
        {
            _logger.LogError("Fastboot executable not found at: {Path}", _fastbootPath);
            return (false, string.Empty, $"Fastboot not found at {_fastbootPath}");
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = _fastbootPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(_fastbootPath) ?? AppContext.BaseDirectory
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        using var process = new Process { StartInfo = processStartInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
                _logger.LogDebug("Fastboot output: {Output}", e.Data);

                // Parse progress if flashing
                if (progress != null && e.Data.Contains(FastbootSendingKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    progress.Report(new UnlockProgress
                    {
                        Step = 3,
                        Stage = "Flashing",
                        Percentage = 50,
                        Message = e.Data
                    });
                }
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);
                _logger.LogDebug("Fastboot error: {Error}", e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            var success = process.ExitCode == 0;
            _logger.LogDebug("Fastboot command completed: {ExitCode}", process.ExitCode);

            return (success, output, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute fastboot command: {Command}", arguments);
            return (false, string.Empty, ex.Message);
        }
    }
}
