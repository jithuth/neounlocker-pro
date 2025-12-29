using System.Text.RegularExpressions;

namespace NEOUnlocker.Client.Helpers;

/// <summary>
/// Helper class for AT command constants and response parsing.
/// </summary>
public static class ATCommandHelper
{
    // AT Commands
    public const string CMD_MANUFACTURER = "AT+CGMI";
    public const string CMD_MODEL = "AT+CGMM";
    public const string CMD_IMEI = "AT+CGSN";
    public const string CMD_FIRMWARE = "AT+CGMR";
    public const string CMD_DEVICE_INFO = "ATI";
    public const string CMD_LOCK_STATUS = "AT^CARDLOCK?";
    public const string CMD_TEST = "AT";

    // Response patterns
    public const string RESPONSE_OK = "OK";
    public const string RESPONSE_ERROR = "ERROR";
    public const string RESPONSE_CME_ERROR = "+CME ERROR";
    public const string RESPONSE_CMS_ERROR = "+CMS ERROR";

    /// <summary>
    /// Parses a simple AT command response (single line result).
    /// </summary>
    public static string ParseSimpleResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;

        // Split by newlines and get the first non-empty, non-OK, non-ERROR line
        var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) &&
                !trimmed.Equals("OK", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("AT", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.Contains("ERROR"))
            {
                return trimmed;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Parses lock status response.
    /// </summary>
    public static string ParseLockStatus(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return "Unknown";

        // Example: ^CARDLOCK: 1,10,0
        var match = Regex.Match(response, @"\^CARDLOCK:\s*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value switch
            {
                "0" => "Unlocked",
                "1" => "Locked",
                "2" => "SIM Lock",
                _ => $"Status: {match.Groups[1].Value}"
            };
        }

        return "Unknown";
    }

    /// <summary>
    /// Checks if a response indicates an error.
    /// </summary>
    public static bool IsErrorResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return true;

        return response.Contains(RESPONSE_ERROR, StringComparison.OrdinalIgnoreCase) ||
               response.Contains(RESPONSE_CME_ERROR, StringComparison.OrdinalIgnoreCase) ||
               response.Contains(RESPONSE_CMS_ERROR, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a response indicates success.
    /// </summary>
    public static bool IsSuccessResponse(string response)
    {
        return !string.IsNullOrWhiteSpace(response) &&
               response.Contains(RESPONSE_OK, StringComparison.OrdinalIgnoreCase) &&
               !IsErrorResponse(response);
    }

    /// <summary>
    /// Extracts the error code from an error response.
    /// </summary>
    public static string? GetErrorCode(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        var match = Regex.Match(response, @"ERROR:?\s*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        match = Regex.Match(response, @"\+CME ERROR:?\s*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        return null;
    }
}
