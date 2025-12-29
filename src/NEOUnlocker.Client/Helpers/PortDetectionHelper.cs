using System.IO.Ports;
using System.Management;

namespace NEOUnlocker.Client.Helpers;

/// <summary>
/// Helper class for detecting COM ports and identifying Huawei devices.
/// </summary>
public static class PortDetectionHelper
{
    /// <summary>
    /// Gets all available COM ports.
    /// </summary>
    public static string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
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
                    var match = System.Text.RegularExpressions.Regex.Match(caption, @"COM(\d+)");
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
