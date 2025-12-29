using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;
using NEOUnlocker.Client.Models;

namespace NEOUnlocker.Client.Helpers;

/// <summary>
/// Helper class for detecting COM ports and identifying Huawei devices.
/// </summary>
public static class PortDetectionHelper
{
    private const string HUAWEI_VID = "12D1";

    /// <summary>
    /// Gets all available COM ports.
    /// </summary>
    public static string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }

    /// <summary>
    /// Gets detailed information about Huawei ports only.
    /// Filters by Huawei VID (12D1) and identifies port types.
    /// </summary>
    public static async Task<List<HuaweiPortInfo>> GetHuaweiPortsAsync()
    {
        return await Task.Run(() =>
        {
            var huaweiPorts = new List<HuaweiPortInfo>();

            try
            {
                // Query Win32_PnPEntity for USB devices with Huawei VID
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE PNPDeviceID LIKE '%VID_12D1%'");

                foreach (ManagementObject obj in searcher.Get())
                {
                    var caption = obj["Caption"]?.ToString() ?? string.Empty;
                    var deviceId = obj["PNPDeviceID"]?.ToString() ?? string.Empty;

                    // Only process if it has a COM port
                    if (!caption.Contains("COM", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Extract COM port number from caption
                    var portMatch = Regex.Match(caption, @"\(COM(\d+)\)");
                    if (!portMatch.Success)
                        continue;

                    var portName = $"COM{portMatch.Groups[1].Value}";

                    // Extract VID and PID from DeviceID
                    // Format: USB\VID_12D1&PID_1004&MI_02\7&2A8C5D9D&0&0002
                    var vidMatch = Regex.Match(deviceId, @"VID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
                    var pidMatch = Regex.Match(deviceId, @"PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);

                    var vid = vidMatch.Success ? vidMatch.Groups[1].Value : string.Empty;
                    var pid = pidMatch.Success ? pidMatch.Groups[1].Value : string.Empty;

                    // Identify port type from description and PID
                    var portType = IdentifyPortType(caption, pid);

                    // Check if port is available
                    var isAvailable = IsPortAvailable(portName);

                    huaweiPorts.Add(new HuaweiPortInfo
                    {
                        PortName = portName,
                        PortType = portType,
                        Description = caption,
                        VID = vid,
                        PID = pid,
                        IsAvailable = isAvailable
                    });
                }
            }
            catch (Exception)
            {
                // WMI query failed, return empty list
            }

            return huaweiPorts;
        });
    }

    /// <summary>
    /// Attempts to detect Huawei 3G PC UI Interface ports using WMI.
    /// </summary>
    public static List<string> DetectHuaweiPorts()
    {
        var huaweiPorts = new List<string>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%COM%'");

            foreach (ManagementObject obj in searcher.Get())
            {
                var caption = obj["Caption"]?.ToString() ?? string.Empty;
                var deviceId = obj["DeviceID"]?.ToString() ?? string.Empty;

                // Look for Huawei devices or 3G PC UI Interface
                if (caption.Contains("Huawei", StringComparison.OrdinalIgnoreCase) ||
                    caption.Contains("3G PC UI Interface", StringComparison.OrdinalIgnoreCase) ||
                    deviceId.Contains("VID_12D1", StringComparison.OrdinalIgnoreCase)) // Huawei VID
                {
                    // Extract COM port number from caption
                    var match = Regex.Match(caption, @"COM(\d+)");
                    if (match.Success)
                    {
                        huaweiPorts.Add($"COM{match.Groups[1].Value}");
                    }
                }
            }
        }
        catch (Exception)
        {
            // WMI query failed, fall back to listing all ports
        }

        return huaweiPorts;
    }

    /// <summary>
    /// Identifies the port type based on description and PID.
    /// </summary>
    public static string IdentifyPortType(string description, string pid)
    {
        // Check description first
        if (description.Contains("3G PC UI Interface", StringComparison.OrdinalIgnoreCase) ||
            description.Contains("PCUI", StringComparison.OrdinalIgnoreCase))
        {
            return "3G_PCUI";
        }

        if (description.Contains("Download", StringComparison.OrdinalIgnoreCase) ||
            description.Contains("Fastboot", StringComparison.OrdinalIgnoreCase))
        {
            return "Download";
        }

        if (description.Contains("Modem", StringComparison.OrdinalIgnoreCase))
        {
            return "Modem";
        }

        // Check PID if description doesn't help
        if (!string.IsNullOrEmpty(pid))
        {
            var pidUpper = pid.ToUpperInvariant();
            
            // PCUI ports
            if (pidUpper == "1003" || pidUpper == "1004")
                return "3G_PCUI";
            
            // Download/Fastboot ports
            if (pidUpper == "1050" || pidUpper == "1057")
                return "Download";
            
            // Modem ports
            if (pidUpper == "1001")
                return "Modem";
        }

        return "Unknown";
    }

    /// <summary>
    /// Checks if a COM port is available (not in use by another application).
    /// </summary>
    public static bool IsPortAvailable(string portName)
    {
        if (!IsValidPortName(portName))
            return false;

        try
        {
            // Try to open the port briefly to check availability
            using var port = new SerialPort(portName);
            port.Open();
            port.Close();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // Port is in use by another application
            return false;
        }
        catch (Exception)
        {
            // Port doesn't exist or other error
            return false;
        }
    }

    /// <summary>
    /// Gets the recommended baud rate for a specific port type.
    /// </summary>
    public static int GetRecommendedBaudRate(string portType)
    {
        return portType switch
        {
            "3G_PCUI" => 115200,
            "Modem" => 115200,
            "Download" => 115200,
            _ => 115200
        };
    }

    /// <summary>
    /// Gets detailed information about a COM port using WMI.
    /// </summary>
    public static string GetPortDescription(string portName)
    {
        // Validate and sanitize port name to prevent WMI injection
        if (string.IsNullOrWhiteSpace(portName) || !IsValidPortName(portName))
            return portName;

        try
        {
            // Escape the port name for WMI query to prevent injection
            var escapedPortName = portName.Replace("\\", "\\\\").Replace("'", "\\'");
            
            using var searcher = new ManagementObjectSearcher(
                $"SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%{escapedPortName}%'");

            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["Caption"]?.ToString() ?? portName;
            }
        }
        catch (Exception)
        {
            // Ignore errors
        }

        return portName;
    }

    /// <summary>
    /// Validates if a port name is valid.
    /// </summary>
    public static bool IsValidPortName(string portName)
    {
        if (string.IsNullOrWhiteSpace(portName))
            return false;

        return portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase) &&
               int.TryParse(portName.Substring(3), out _);
    }
}

