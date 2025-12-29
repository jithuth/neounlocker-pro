using Microsoft.Extensions.Logging;
using NEOUnlocker.Client.Models;
using NEOUnlocker.Client.Services;
using System.Diagnostics;

namespace NEOUnlocker.Client.Forms;

/// <summary>
/// Main application form for the NEOUnlocker Pro router unlock tool.
/// </summary>
public partial class MainForm : Form
{
    private readonly ILogger<MainForm> _logger;
    private readonly ISerialPortService _serialPortService;
    private readonly IFastbootService _fastbootService;
    private readonly IRouterService _routerService;

    private CancellationTokenSource? _cancellationTokenSource;
    private readonly Stopwatch _stopwatch = new();

    public MainForm(
        ILogger<MainForm> logger,
        ISerialPortService serialPortService,
        IFastbootService fastbootService,
        IRouterService routerService)
    {
        _logger = logger;
        _serialPortService = serialPortService;
        _fastbootService = fastbootService;
        _routerService = routerService;

        InitializeComponent();
        
        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        LogInfo("NEOUnlocker Pro started");
        LogInfo("Ready to begin unlock process");
        
        // Auto-scan ports on load
        await ScanPortsAsync();
    }

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        
        if (_serialPortService.IsConnected)
        {
            await _serialPortService.DisconnectAsync();
        }
    }

    #region Port Selection

    private async void BtnScanPorts_Click(object? sender, EventArgs e)
    {
        await ScanPortsAsync();
    }

    private async Task ScanPortsAsync()
    {
        try
        {
            btnScanPorts.Enabled = false;
            LogInfo("Scanning for COM ports...");

            var ports = await _serialPortService.GetAvailablePortsAsync();
            
            cmbPorts.Items.Clear();
            if (ports.Length > 0)
            {
                cmbPorts.Items.AddRange(ports);
                cmbPorts.SelectedIndex = 0;
                LogSuccess($"Found {ports.Length} COM port(s)");
            }
            else
            {
                LogWarning("No COM ports found");
            }
        }
        catch (Exception ex)
        {
            LogError($"Error scanning ports: {ex.Message}");
        }
        finally
        {
            btnScanPorts.Enabled = true;
        }
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        if (_serialPortService.IsConnected)
        {
            await DisconnectAsync();
        }
        else
        {
            await ConnectAsync();
        }
    }

    private async Task ConnectAsync()
    {
        if (cmbPorts.SelectedItem == null)
        {
            LogWarning("Please select a COM port");
            return;
        }

        var portName = cmbPorts.SelectedItem.ToString();
        if (string.IsNullOrEmpty(portName))
            return;

        try
        {
            btnConnect.Enabled = false;
            LogInfo($"Connecting to {portName}...");

            var connected = await _serialPortService.ConnectAsync(portName);
            
            if (connected)
            {
                lblPortStatus.Text = $"Status: Connected to {portName}";
                lblPortStatus.ForeColor = Color.Green;
                btnConnect.Text = "Disconnect";
                grpStep1.Enabled = true;
                LogSuccess($"Connected to {portName}");
            }
            else
            {
                LogError($"Failed to connect to {portName}");
            }
        }
        catch (Exception ex)
        {
            LogError($"Connection error: {ex.Message}");
        }
        finally
        {
            btnConnect.Enabled = true;
        }
    }

    private async Task DisconnectAsync()
    {
        try
        {
            await _serialPortService.DisconnectAsync();
            lblPortStatus.Text = "Status: Disconnected";
            lblPortStatus.ForeColor = Color.Gray;
            btnConnect.Text = "Connect";
            grpStep1.Enabled = false;
            grpStep2.Enabled = false;
            grpStep3.Enabled = false;
            LogInfo("Disconnected from serial port");
        }
        catch (Exception ex)
        {
            LogError($"Disconnect error: {ex.Message}");
        }
    }

    #endregion

    #region Step 1: Read Properties

    private async void BtnReadProperties_Click(object? sender, EventArgs e)
    {
        await ExecuteStep1Async();
    }

    private async Task ExecuteStep1Async()
    {
        if (!_serialPortService.IsConnected)
        {
            LogWarning("Please connect to a COM port first");
            return;
        }

        try
        {
            btnReadProperties.Enabled = false;
            lblStep1Status.Text = "Reading...";
            lblStep1Status.ForeColor = Color.Blue;
            _stopwatch.Restart();

            var progress = new Progress<UnlockProgress>(UpdateProgress);
            var portName = _serialPortService.ConnectedPort ?? "Unknown";

            var routerInfo = await _routerService.ExecuteStep1_ReadPropertiesAsync(portName, progress);

            // Update UI with router info
            txtManufacturer.Text = routerInfo.Manufacturer ?? "N/A";
            txtModel.Text = routerInfo.Model ?? "N/A";
            txtIMEI.Text = routerInfo.IMEI ?? "N/A";
            txtFirmware.Text = routerInfo.FirmwareVersion ?? "N/A";
            txtLockStatus.Text = routerInfo.LockStatus ?? "Unknown";

            lblStep1Status.Text = "Complete";
            lblStep1Status.ForeColor = Color.Green;
            grpStep2.Enabled = true;

            LogSuccess("Step 1 completed successfully");
            LogInfo($"Router: {routerInfo.Model}, IMEI: {routerInfo.IMEI}");
        }
        catch (Exception ex)
        {
            lblStep1Status.Text = "Failed";
            lblStep1Status.ForeColor = Color.Red;
            LogError($"Step 1 failed: {ex.Message}");
        }
        finally
        {
            btnReadProperties.Enabled = true;
        }
    }

    #endregion

    #region Step 2: Detect Fastboot

    private async void BtnDetectFastboot_Click(object? sender, EventArgs e)
    {
        await ExecuteStep2Async();
    }

    private async Task ExecuteStep2Async()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            btnDetectFastboot.Enabled = false;
            lblStep2Status.Text = "Waiting...";
            lblStep2Status.ForeColor = Color.Blue;

            var progress = new Progress<UnlockProgress>(UpdateProgress);

            var detected = await _routerService.ExecuteStep2_SwitchToFastbootAsync(
                progress, 
                _cancellationTokenSource.Token);

            if (detected)
            {
                var serial = await _fastbootService.GetDeviceSerialAsync();
                txtDeviceSerial.Text = serial ?? "Unknown";
                lblStep2Status.Text = "Detected";
                lblStep2Status.ForeColor = Color.Green;
                grpStep3.Enabled = true;
                LogSuccess("Step 2 completed - Fastboot device detected");
            }
            else
            {
                lblStep2Status.Text = "Not Detected";
                lblStep2Status.ForeColor = Color.Red;
                LogError("Step 2 failed - Fastboot device not detected");
            }
        }
        catch (Exception ex)
        {
            lblStep2Status.Text = "Failed";
            lblStep2Status.ForeColor = Color.Red;
            LogError($"Step 2 error: {ex.Message}");
        }
        finally
        {
            btnDetectFastboot.Enabled = true;
            lblCountdown.Text = "";
        }
    }

    #endregion

    #region Step 3: Unlock Device

    private async void BtnStartUnlock_Click(object? sender, EventArgs e)
    {
        await ExecuteStep3Async();
    }

    private async Task ExecuteStep3Async()
    {
        if (_routerService.CurrentRouterInfo == null)
        {
            LogWarning("Please complete Step 1 first");
            return;
        }

        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            btnStartUnlock.Enabled = false;
            lblStep3Status.Text = "In Progress...";
            lblStep3Status.ForeColor = Color.Blue;

            var progress = new Progress<UnlockProgress>(UpdateProgress);

            var success = await _routerService.ExecuteStep3_UnlockAsync(
                progress,
                _cancellationTokenSource.Token);

            if (success)
            {
                lblStep3Status.Text = "Complete";
                lblStep3Status.ForeColor = Color.Green;
                LogSuccess("Step 3 completed - Device unlocked successfully!");
                MessageBox.Show(
                    "Device unlocked successfully!\n\nThe device will now reboot.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                lblStep3Status.Text = "Failed";
                lblStep3Status.ForeColor = Color.Red;
                LogError("Step 3 failed - Unlock unsuccessful");
            }
        }
        catch (Exception ex)
        {
            lblStep3Status.Text = "Failed";
            lblStep3Status.ForeColor = Color.Red;
            LogError($"Step 3 error: {ex.Message}");
        }
        finally
        {
            btnStartUnlock.Enabled = true;
            _stopwatch.Stop();
        }
    }

    #endregion

    #region Progress Reporting

    private void UpdateProgress(UnlockProgress progress)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<UnlockProgress>(UpdateProgress), progress);
            return;
        }

        // Update status bar
        lblCurrentStep.Text = $"Current Step: {progress.Step} - {progress.Stage}";
        progressStatusBar.Value = Math.Min(progress.Percentage, 100);

        // Update step-specific controls
        switch (progress.Step)
        {
            case 1:
                lblStep1Status.Text = progress.Stage;
                break;
            case 2:
                lblStep2Status.Text = progress.Stage;
                lblCountdown.Text = progress.Message.Contains("remaining") ? 
                    progress.Message.Split('(').LastOrDefault()?.TrimEnd(')') ?? "" : "";
                break;
            case 3:
                progressBar.Value = Math.Min(progress.Percentage, 100);
                lblOperation.Text = progress.Stage;
                if (progress.SpeedMBps.HasValue)
                {
                    lblSpeed.Text = $"{progress.SpeedMBps.Value:F2} MB/s";
                }
                break;
        }

        // Update elapsed time
        lblElapsedTime.Text = $"Time: {_stopwatch.Elapsed:hh\\:mm\\:ss}";

        // Log progress
        if (!string.IsNullOrWhiteSpace(progress.Message))
        {
            LogInfo(progress.Message);
        }
    }

    #endregion

    #region Logging

    private void LogInfo(string message)
    {
        LogMessage(message, Color.Black);
    }

    private void LogSuccess(string message)
    {
        LogMessage(message, Color.Green);
    }

    private void LogWarning(string message)
    {
        LogMessage(message, Color.Orange);
    }

    private void LogError(string message)
    {
        LogMessage(message, Color.Red);
    }

    private void LogMessage(string message, Color color)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string, Color>(LogMessage), message, color);
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        txtLogs.SelectionStart = txtLogs.TextLength;
        txtLogs.SelectionLength = 0;

        txtLogs.SelectionColor = Color.Gray;
        txtLogs.AppendText($"[{timestamp}] ");

        txtLogs.SelectionColor = color;
        txtLogs.AppendText($"{message}\n");

        txtLogs.SelectionColor = txtLogs.ForeColor;
        txtLogs.ScrollToCaret();

        _logger.LogInformation(message);
    }

    #endregion
}
