using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Logging;
using NEOUnlocker.Client.Helpers;
using NEOUnlocker.Client.Models;

namespace NEOUnlocker.Client.Services;

/// <summary>
/// Service for serial port communication with routers via AT commands.
/// </summary>
public class SerialPortService : ISerialPortService, IDisposable
{
    private readonly ILogger<SerialPortService> _logger;
    private SerialPort? _serialPort;
    private readonly SemaphoreSlim _commandLock = new(1, 1);
    private int _currentBaudRate = 115200;
    private DateTime _lastCommandTime;
    private string _lastCommand = string.Empty;
    private int _lastResponseTimeMs;

    public bool IsConnected => _serialPort?.IsOpen ?? false;
    public string? ConnectedPort { get; private set; }
    public int CurrentBaudRate => _currentBaudRate;

    public SerialPortService(ILogger<SerialPortService> logger)
    {
        _logger = logger;
    }

    public Task<string[]> GetAvailablePortsAsync()
    {
        return Task.Run(() =>
        {
            var ports = PortDetectionHelper.GetAvailablePorts();
            _logger.LogInformation("Found {Count} COM ports", ports.Length);
            return ports;
        });
    }

    public async Task<List<HuaweiPortInfo>> GetHuaweiPortsAsync()
    {
        _logger.LogInformation("Scanning for Huawei devices...");
        var ports = await PortDetectionHelper.GetHuaweiPortsAsync();
        _logger.LogInformation("Found {Count} Huawei port(s)", ports.Count);
        return ports;
    }

    public async Task<bool> ConnectAsync(string portName, int baudRate = 115200)
    {
        await _commandLock.WaitAsync();
        try
        {
            if (_serialPort?.IsOpen == true)
            {
                _logger.LogWarning("Port already open, closing existing connection");
                await DisconnectAsync();
            }

            // Check if port is available
            if (!PortDetectionHelper.IsPortAvailable(portName))
            {
                _logger.LogError("Port {Port} is already in use by another application", portName);
                throw new InvalidOperationException(
                    $"Port {portName} is currently in use by another application.\n\n" +
                    "Possible causes:\n" +
                    "• Huawei Mobile Connect software is running\n" +
                    "• Another unlock tool is open\n" +
                    "• Windows Modem Manager has the port locked\n\n" +
                    "Solutions:\n" +
                    "1. Close Huawei Mobile Connect software\n" +
                    "2. Unplug and replug the device\n" +
                    "3. Restart this application\n" +
                    "4. Try a different USB port");
            }

            _logger.LogInformation("Connecting to {Port} at {BaudRate} baud", portName, baudRate);

            _serialPort = new SerialPort(portName)
            {
                BaudRate = baudRate,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                
                // CRITICAL: Enable DTR and RTS for Huawei devices
                DtrEnable = true,
                RtsEnable = true,
                
                ReadTimeout = 5000,
                WriteTimeout = 5000,
                NewLine = "\r\n",
                Encoding = Encoding.ASCII,
                
                // Buffer sizes
                ReadBufferSize = 4096,
                WriteBufferSize = 2048
            };

            _serialPort.Open();
            _currentBaudRate = baudRate;
            ConnectedPort = portName;

            // Clear buffers
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            // Wait for device to be ready
            await Task.Delay(500);

            // Test connection with AT command
            var response = await SendCommandAsync(ATCommandHelper.CMD_TEST, 3000);

            if (ATCommandHelper.IsSuccessResponse(response))
            {
                _logger.LogInformation("Successfully connected to {Port} at {BaudRate} baud", portName, baudRate);
                return true;
            }

            _logger.LogWarning("Port opened but device not responding to AT commands");
            await DisconnectAsync();
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied to port {Port}", portName);
            ConnectedPort = null;
            _serialPort?.Dispose();
            _serialPort = null;
            throw new InvalidOperationException(
                $"Access denied to port {portName}.\n\n" +
                "The port is in use by another application or you don't have permission to access it.\n\n" +
                "Try closing other applications that might be using the port.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to {Port}", portName);
            ConnectedPort = null;
            _serialPort?.Dispose();
            _serialPort = null;
            throw;
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public async Task<bool> ConnectWithRetryAsync(string portName, int maxRetries = 3)
    {
        var baudRates = new[] { 115200, 57600, 9600 };
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Use different baud rates on different attempts
                var baudRate = baudRates[(attempt - 1) % baudRates.Length];
                _logger.LogInformation("Connection attempt {Attempt}/{Max} with baud rate {BaudRate}", 
                    attempt, maxRetries, baudRate);
                
                var connected = await ConnectAsync(portName, baudRate);
                if (connected)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Connection attempt {Attempt}/{Max} failed", attempt, maxRetries);
                
                if (attempt < maxRetries)
                {
                    // Wait before retry
                    await Task.Delay(1000);
                }
            }
        }
        
        _logger.LogError("Failed to connect after {MaxRetries} attempts", maxRetries);
        return false;
    }

    public async Task DisconnectAsync()
    {
        await _commandLock.WaitAsync();
        try
        {
            if (_serialPort != null)
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _serialPort.Dispose();
                _serialPort = null;
            }

            _logger.LogInformation("Disconnected from {Port}", ConnectedPort);
            ConnectedPort = null;
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public async Task<string> SendCommandAsync(string command, int timeoutMs = 5000)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
        {
            throw new InvalidOperationException("Port is not open");
        }

        await _commandLock.WaitAsync();
        try
        {
            var startTime = DateTime.UtcNow;
            _lastCommand = command;
            _lastCommandTime = startTime;

            // Clear buffers before sending
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            // Send command
            _logger.LogDebug("Sending command: {Command}", command);
            _serialPort.WriteLine(command);

            // Read response with timeout
            var response = new StringBuilder(1024); // Pre-sized for typical AT response
            var timeoutSpan = TimeSpan.FromMilliseconds(timeoutMs);

            while ((DateTime.UtcNow - startTime) < timeoutSpan)
            {
                if (_serialPort.BytesToRead > 0)
                {
                    var line = _serialPort.ReadLine();
                    response.AppendLine(line);

                    // Check if we've received a complete response
                    var responseStr = response.ToString();
                    if (responseStr.Contains("OK") || responseStr.Contains("ERROR"))
                    {
                        break;
                    }
                }
                else
                {
                    await Task.Delay(50);
                }
            }

            var result = response.ToString().Trim();
            _lastResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            _logger.LogDebug("Received response in {ResponseTime}ms: {Response}", 
                _lastResponseTimeMs, 
                result.Length > 100 ? result[..100] + "..." : result);

            return result;
        }
        catch (TimeoutException)
        {
            _logger.LogError("Command timed out: {Command}", command);
            return "ERROR: Timeout";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending command: {Command}", command);
            return $"ERROR: {ex.Message}";
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public async Task<RouterInfo> ReadRouterPropertiesAsync()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected to any port");
        }

        _logger.LogInformation("Reading router properties...");

        var routerInfo = new RouterInfo
        {
            ReadTimestamp = DateTime.UtcNow
        };

        try
        {
            // Read manufacturer
            var response = await SendCommandAsync(ATCommandHelper.CMD_MANUFACTURER);
            if (ATCommandHelper.IsSuccessResponse(response))
            {
                routerInfo.Manufacturer = ATCommandHelper.ParseSimpleResponse(response);
                _logger.LogInformation("Manufacturer: {Manufacturer}", routerInfo.Manufacturer);
            }

            await Task.Delay(200);

            // Read model
            response = await SendCommandAsync(ATCommandHelper.CMD_MODEL);
            if (ATCommandHelper.IsSuccessResponse(response))
            {
                routerInfo.Model = ATCommandHelper.ParseSimpleResponse(response);
                _logger.LogInformation("Model: {Model}", routerInfo.Model);
            }

            await Task.Delay(200);

            // Read IMEI
            response = await SendCommandAsync(ATCommandHelper.CMD_IMEI);
            if (ATCommandHelper.IsSuccessResponse(response))
            {
                routerInfo.IMEI = ATCommandHelper.ParseSimpleResponse(response);
                _logger.LogInformation("IMEI: {IMEI}", routerInfo.IMEI);
            }

            await Task.Delay(200);

            // Read firmware version
            response = await SendCommandAsync(ATCommandHelper.CMD_FIRMWARE);
            if (ATCommandHelper.IsSuccessResponse(response))
            {
                routerInfo.FirmwareVersion = ATCommandHelper.ParseSimpleResponse(response);
                _logger.LogInformation("Firmware: {Firmware}", routerInfo.FirmwareVersion);
            }

            await Task.Delay(200);

            // Read device info
            response = await SendCommandAsync(ATCommandHelper.CMD_DEVICE_INFO);
            if (ATCommandHelper.IsSuccessResponse(response))
            {
                routerInfo.DeviceInfo = ATCommandHelper.ParseSimpleResponse(response);
            }

            await Task.Delay(200);

            // Read lock status
            response = await SendCommandAsync(ATCommandHelper.CMD_LOCK_STATUS);
            if (ATCommandHelper.IsSuccessResponse(response))
            {
                routerInfo.LockStatus = ATCommandHelper.ParseLockStatus(response);
                _logger.LogInformation("Lock Status: {LockStatus}", routerInfo.LockStatus);
            }
            else
            {
                routerInfo.LockStatus = "Unknown";
            }

            _logger.LogInformation("Successfully read router properties");
            return routerInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read router properties");
            throw;
        }
    }

    public string GetDiagnostics()
    {
        if (!IsConnected)
        {
            return "Status: Not Connected";
        }

        var dtr = _serialPort?.DtrEnable ?? false;
        var rts = _serialPort?.RtsEnable ?? false;

        return $"Port: {ConnectedPort}\n" +
               $"Baud Rate: {_currentBaudRate}\n" +
               $"Status: Connected ✅\n" +
               $"Signals: DTR={GetSignalText(dtr)}, RTS={GetSignalText(rts)}\n" +
               $"Last Command: {_lastCommand}\n" +
               $"Response Time: {_lastResponseTimeMs}ms";
    }

    private static string GetSignalText(bool enabled)
    {
        return enabled ? "ON" : "OFF";
    }

    public void Dispose()
    {
        _serialPort?.Dispose();
        _commandLock?.Dispose();
    }
}
