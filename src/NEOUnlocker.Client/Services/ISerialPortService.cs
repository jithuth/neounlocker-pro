using NEOUnlocker.Client.Models;

namespace NEOUnlocker.Client.Services;

/// <summary>
/// Interface for serial port communication with routers via AT commands.
/// </summary>
public interface ISerialPortService
{
    /// <summary>
    /// Gets all available COM ports.
    /// </summary>
    Task<string[]> GetAvailablePortsAsync();

    /// <summary>
    /// Connects to the specified COM port.
    /// </summary>
    Task<bool> ConnectAsync(string portName, int baudRate = 115200);

    /// <summary>
    /// Disconnects from the current port.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Sends an AT command and waits for response.
    /// </summary>
    Task<string> SendCommandAsync(string command, int timeoutMs = 5000);

    /// <summary>
    /// Reads router properties using AT commands.
    /// </summary>
    Task<RouterInfo> ReadRouterPropertiesAsync();

    /// <summary>
    /// Gets whether the port is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the name of the currently connected port.
    /// </summary>
    string? ConnectedPort { get; }
}
