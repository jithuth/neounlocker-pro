namespace NEOUnlocker.Server.Models;

/// <summary>
/// Request to create a new flash session.
/// </summary>
public class CreateFlashSessionRequest
{
    /// <summary>
    /// Hardware ID of the client device.
    /// </summary>
    public required string HWID { get; init; }
    
    /// <summary>
    /// Device type to flash (e.g., MTK6580, Qualcomm8937).
    /// </summary>
    public required string DeviceType { get; init; }
    
    /// <summary>
    /// Client's RSA public key in PEM format for wrapping the session key.
    /// </summary>
    public required string ClientPublicKeyPem { get; init; }
}

/// <summary>
/// Response containing flash session details.
/// </summary>
public class FlashSessionResponse
{
    /// <summary>
    /// Unique identifier for the session.
    /// </summary>
    public required string SessionId { get; init; }
    
    /// <summary>
    /// Session key wrapped with client's RSA public key (Base64 encoded).
    /// </summary>
    public required string WrappedSessionKeyBase64 { get; init; }
    
    /// <summary>
    /// Timestamp when the session expires (ISO 8601).
    /// </summary>
    public required string ExpiresAt { get; init; }
    
    /// <summary>
    /// Current status of the session.
    /// </summary>
    public required string Status { get; init; }
    
    /// <summary>
    /// List of firmware files available for download.
    /// </summary>
    public required List<string> FirmwareFiles { get; init; }
    
    /// <summary>
    /// Number of credits that will be deducted.
    /// </summary>
    public int CreditCost { get; init; }
}

/// <summary>
/// Request to complete a flash session.
/// </summary>
public class CompleteFlashSessionRequest
{
    /// <summary>
    /// Hardware ID of the client device for validation.
    /// </summary>
    public required string HWID { get; init; }
    
    /// <summary>
    /// Whether the flash operation succeeded.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Optional error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Response for session completion.
/// </summary>
public class CompleteFlashSessionResponse
{
    /// <summary>
    /// Whether the session was successfully completed.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Status message.
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// Whether credits were deducted.
    /// </summary>
    public bool CreditsDeducted { get; init; }
}
