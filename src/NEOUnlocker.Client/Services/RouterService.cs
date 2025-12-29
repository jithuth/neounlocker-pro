using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NEOUnlocker.Client.Models;

namespace NEOUnlocker.Client.Services;

/// <summary>
/// Service that orchestrates the three-step router unlock process.
/// </summary>
public class RouterService : IRouterService
{
    private readonly ILogger<RouterService> _logger;
    private readonly ISerialPortService _serialPortService;
    private readonly IFastbootService _fastbootService;
    private readonly IConfiguration _configuration;

    public RouterInfo? CurrentRouterInfo { get; private set; }
    public int CurrentStep { get; private set; }

    public RouterService(
        ILogger<RouterService> logger,
        ISerialPortService serialPortService,
        IFastbootService fastbootService,
        IConfiguration configuration)
    {
        _logger = logger;
        _serialPortService = serialPortService;
        _fastbootService = fastbootService;
        _configuration = configuration;
    }

    public async Task<RouterInfo> ExecuteStep1_ReadPropertiesAsync(
        string comPort,
        IProgress<UnlockProgress>? progress = null)
    {
        CurrentStep = 1;
        _logger.LogInformation("=== Step 1: Reading Router Properties ===");

        try
        {
            progress?.Report(new UnlockProgress
            {
                Step = 1,
                Stage = "Connecting",
                Percentage = 0,
                Message = $"Connecting to {comPort}..."
            });

            // Connect to COM port
            var connected = await _serialPortService.ConnectAsync(comPort);
            if (!connected)
            {
                throw new InvalidOperationException($"Failed to connect to {comPort}");
            }

            progress?.Report(new UnlockProgress
            {
                Step = 1,
                Stage = "Connected",
                Percentage = 20,
                Message = $"Connected to {comPort}"
            });

            // Read router properties
            progress?.Report(new UnlockProgress
            {
                Step = 1,
                Stage = "Reading Properties",
                Percentage = 40,
                Message = "Sending AT commands to read device information..."
            });

            CurrentRouterInfo = await _serialPortService.ReadRouterPropertiesAsync();

            progress?.Report(new UnlockProgress
            {
                Step = 1,
                Stage = "Complete",
                Percentage = 100,
                Message = $"Successfully read properties for {CurrentRouterInfo.Model}"
            });

            _logger.LogInformation("Step 1 completed successfully");
            return CurrentRouterInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Step 1 failed");
            progress?.Report(new UnlockProgress
            {
                Step = 1,
                Stage = "Failed",
                Percentage = 0,
                Message = $"Error: {ex.Message}"
            });
            throw;
        }
    }

    public async Task<bool> ExecuteStep2_SwitchToFastbootAsync(
        IProgress<UnlockProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        CurrentStep = 2;
        _logger.LogInformation("=== Step 2: Switching to Fastboot Mode ===");

        try
        {
            // Disconnect from serial port
            if (_serialPortService.IsConnected)
            {
                progress?.Report(new UnlockProgress
                {
                    Step = 2,
                    Stage = "Disconnecting",
                    Percentage = 0,
                    Message = "Disconnecting from 3G PC UI Interface..."
                });

                await _serialPortService.DisconnectAsync();
                await Task.Delay(1000, cancellationToken);
            }

            progress?.Report(new UnlockProgress
            {
                Step = 2,
                Stage = "Waiting",
                Percentage = 10,
                Message = "Please switch your device to Fastboot mode and connect via Huawei Download port..."
            });

            // Wait for fastboot device with timeout
            var timeoutSeconds = _configuration.GetValue("Fastboot:DetectionTimeoutSeconds", 60);
            var startTime = DateTime.UtcNow;
            var detected = false;

            while ((DateTime.UtcNow - startTime).TotalSeconds < timeoutSeconds && !cancellationToken.IsCancellationRequested)
            {
                detected = await _fastbootService.DetectDeviceAsync();
                if (detected)
                    break;

                var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                var percentage = (int)((elapsed / timeoutSeconds) * 80) + 10;

                progress?.Report(new UnlockProgress
                {
                    Step = 2,
                    Stage = "Detecting",
                    Percentage = percentage,
                    Message = $"Waiting for fastboot device... ({(int)(timeoutSeconds - elapsed)}s remaining)"
                });

                await Task.Delay(2000, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Step 2 cancelled by user");
                return false;
            }

            if (!detected)
            {
                _logger.LogError("Fastboot device not detected within timeout");
                progress?.Report(new UnlockProgress
                {
                    Step = 2,
                    Stage = "Failed",
                    Percentage = 0,
                    Message = "Timeout: Fastboot device not detected"
                });
                return false;
            }

            // Get device serial
            var serial = await _fastbootService.GetDeviceSerialAsync();

            progress?.Report(new UnlockProgress
            {
                Step = 2,
                Stage = "Complete",
                Percentage = 100,
                Message = $"Fastboot device detected: {serial ?? "Unknown"}"
            });

            _logger.LogInformation("Step 2 completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Step 2 failed");
            progress?.Report(new UnlockProgress
            {
                Step = 2,
                Stage = "Failed",
                Percentage = 0,
                Message = $"Error: {ex.Message}"
            });
            return false;
        }
    }

    public async Task<bool> ExecuteStep3_UnlockAsync(
        IProgress<UnlockProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        CurrentStep = 3;
        _logger.LogInformation("=== Step 3: Unlocking Device ===");

        try
        {
            if (CurrentRouterInfo == null)
            {
                throw new InvalidOperationException("Step 1 must be completed first");
            }

            progress?.Report(new UnlockProgress
            {
                Step = 3,
                Stage = "Preparing",
                Percentage = 0,
                Message = "Preparing to flash bootloader..."
            });

            // Get bootloader path based on model
            var bootloaderPath = GetBootloaderPath(CurrentRouterInfo.Model);
            if (string.IsNullOrEmpty(bootloaderPath) || !File.Exists(bootloaderPath))
            {
                var message = string.IsNullOrEmpty(bootloaderPath)
                    ? $"No bootloader mapping found for model: {CurrentRouterInfo.Model}"
                    : $"Bootloader file not found: {bootloaderPath}";

                _logger.LogError(message);
                progress?.Report(new UnlockProgress
                {
                    Step = 3,
                    Stage = "Failed",
                    Percentage = 0,
                    Message = message
                });
                return false;
            }

            _logger.LogInformation("Using bootloader: {Path}", bootloaderPath);

            progress?.Report(new UnlockProgress
            {
                Step = 3,
                Stage = "Flashing",
                Percentage = 20,
                Message = $"Flashing bootloader: {Path.GetFileName(bootloaderPath)}"
            });

            // Flash bootloader
            var flashed = await _fastbootService.FlashBootloaderAsync(bootloaderPath, progress);
            if (!flashed)
            {
                _logger.LogError("Failed to flash bootloader");
                progress?.Report(new UnlockProgress
                {
                    Step = 3,
                    Stage = "Failed",
                    Percentage = 0,
                    Message = "Failed to flash bootloader"
                });
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
                return false;

            progress?.Report(new UnlockProgress
            {
                Step = 3,
                Stage = "Unlocking",
                Percentage = 70,
                Message = "Executing unlock command..."
            });

            // Unlock device
            var unlocked = await _fastbootService.UnlockDeviceAsync();
            if (!unlocked)
            {
                _logger.LogWarning("Unlock command failed, device may already be unlocked");
            }

            if (cancellationToken.IsCancellationRequested)
                return false;

            progress?.Report(new UnlockProgress
            {
                Step = 3,
                Stage = "Rebooting",
                Percentage = 90,
                Message = "Rebooting device..."
            });

            // Reboot device
            await _fastbootService.RebootDeviceAsync();

            progress?.Report(new UnlockProgress
            {
                Step = 3,
                Stage = "Complete",
                Percentage = 100,
                Message = "Device unlocked successfully!"
            });

            _logger.LogInformation("Step 3 completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Step 3 failed");
            progress?.Report(new UnlockProgress
            {
                Step = 3,
                Stage = "Failed",
                Percentage = 0,
                Message = $"Error: {ex.Message}"
            });
            return false;
        }
    }

    private string? GetBootloaderPath(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return null;

        var bootloadersDir = _configuration["Bootloaders:Directory"] ?? "./Resources/Bootloaders";
        if (!Path.IsPathRooted(bootloadersDir))
        {
            bootloadersDir = Path.Combine(AppContext.BaseDirectory, bootloadersDir);
        }

        // Try to get from mapping
        var mapping = _configuration.GetSection("Bootloaders:ModelMapping");
        var bootloaderFile = mapping[model];

        if (!string.IsNullOrEmpty(bootloaderFile))
        {
            return Path.Combine(bootloadersDir, bootloaderFile);
        }

        // Try direct match with .bin extension
        var directPath = Path.Combine(bootloadersDir, $"{model}.bin");
        if (File.Exists(directPath))
        {
            return directPath;
        }

        _logger.LogWarning("No bootloader found for model: {Model}", model);
        return null;
    }
}
