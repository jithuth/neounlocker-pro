using System.Management;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NEOUnlocker.Client.Services;

/// <summary>
/// Implementation of hardware ID service.
/// Generates a unique hardware ID based on system components.
/// </summary>
public class HWIDService : IHWIDService
{
    private readonly ILogger<HWIDService> _logger;
    private string? _cachedHwid;

    public HWIDService(ILogger<HWIDService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string GetHardwareId()
    {
        if (_cachedHwid != null)
        {
            return _cachedHwid;
        }

        try
        {
            var components = new List<string>();

            // Get CPU ID
            components.Add(GetProcessorId());

            // Get Motherboard Serial
            components.Add(GetMotherboardSerial());

            // Get BIOS Serial
            components.Add(GetBiosSerial());

            // Combine and hash all components
            var combined = string.Join("|", components);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
            
            _cachedHwid = Convert.ToHexString(hash);
            
            _logger.LogInformation("Generated HWID successfully");
            
            return _cachedHwid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate HWID, using fallback");
            
            // Fallback: use machine name and username hash
            var fallback = $"{Environment.MachineName}|{Environment.UserName}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fallback));
            _cachedHwid = Convert.ToHexString(hash);
            
            return _cachedHwid;
        }
    }

    private string GetProcessorId()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                var processorId = obj["ProcessorId"]?.ToString();
                if (!string.IsNullOrEmpty(processorId))
                {
                    return processorId;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get processor ID");
        }
        
        return "UNKNOWN_CPU";
    }

    private string GetMotherboardSerial()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            foreach (ManagementObject obj in searcher.Get())
            {
                var serial = obj["SerialNumber"]?.ToString();
                if (!string.IsNullOrEmpty(serial))
                {
                    return serial;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get motherboard serial");
        }
        
        return "UNKNOWN_MB";
    }

    private string GetBiosSerial()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
            foreach (ManagementObject obj in searcher.Get())
            {
                var serial = obj["SerialNumber"]?.ToString();
                if (!string.IsNullOrEmpty(serial))
                {
                    return serial;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get BIOS serial");
        }
        
        return "UNKNOWN_BIOS";
    }
}
