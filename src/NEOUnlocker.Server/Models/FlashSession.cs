namespace NEOUnlocker.Server.Models;

/// <summary>
/// Represents the status of a flash session.
/// </summary>
public enum FlashSessionStatus
{
    /// <summary>
    /// Session is active and ready for use.
    /// </summary>
    Active,
    
    /// <summary>
    /// Session completed successfully.
    /// </summary>
    Completed,
    
    /// <summary>
    /// Session failed during execution.
    /// </summary>
    Failed,
    
    /// <summary>
    /// Session expired due to timeout.
    /// </summary>
    Expired,
    
    /// <summary>
    /// Session was burned (invalidated after use).
    /// </summary>
    Burned
}

/// <summary>
/// Represents a one-time flash session with cryptographic session key.
/// </summary>
public class FlashSession
{
    /// <summary>
    /// Unique identifier for the session.
    /// </summary>
    public required string SessionId { get; init; }
    
    /// <summary>
    /// Hardware ID of the client device.
    /// </summary>
    public required string HWID { get; init; }
    
    /// <summary>
    /// Device type to flash (e.g., MTK6580, Qualcomm8937).
    /// </summary>
    public required string DeviceType { get; init; }
    
    /// <summary>
    /// AES-256-GCM session key (32 bytes) for encrypting firmware.
    /// This key is wrapped with the client's RSA public key before transmission.
    /// </summary>
    public required byte[] SessionKey { get; init; }
    
    /// <summary>
    /// Session key wrapped with client's RSA public key (RSA-OAEP-SHA256).
    /// </summary>
    public required byte[] WrappedSessionKey { get; init; }
    
    /// <summary>
    /// Timestamp when the session was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// Timestamp when the session expires.
    /// </summary>
    public DateTime ExpiresAt { get; init; }
    
    /// <summary>
    /// Current status of the session.
    /// </summary>
    public FlashSessionStatus Status { get; set; }
    
    /// <summary>
    /// List of firmware files required for this device type.
    /// </summary>
    public required List<string> RequiredFirmwareFiles { get; init; }
    
    /// <summary>
    /// Number of credits to deduct upon session completion.
    /// </summary>
    public int CreditCost { get; init; }
    
    /// <summary>
    /// Optional error message if the session failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Checks if the session is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    
    /// <summary>
    /// Checks if the session is usable (active and not expired).
    /// </summary>
    public bool IsUsable => Status == FlashSessionStatus.Active && !IsExpired;
}
