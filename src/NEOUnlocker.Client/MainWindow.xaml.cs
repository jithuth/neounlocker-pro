using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NEOUnlocker.Client.Services;

namespace NEOUnlocker.Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly IFlashClient _flashClient;
    private readonly IHWIDService _hwidService;
    private CancellationTokenSource? _cancellationTokenSource;
    
    private bool _isFlashing;
    public bool IsFlashing
    {
        get => _isFlashing;
        set
        {
            _isFlashing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotFlashing));
        }
    }
    
    public bool IsNotFlashing => !IsFlashing;

    public MainWindow(IFlashClient flashClient, IHWIDService hwidService)
    {
        InitializeComponent();
        
        _flashClient = flashClient;
        _hwidService = hwidService;
        
        DataContext = this;
        
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Load hardware ID
        try
        {
            var hwid = _hwidService.GetHardwareId();
            HwidTextBox.Text = hwid;
            AppendLog("Hardware ID loaded successfully");
        }
        catch (Exception ex)
        {
            HwidTextBox.Text = "Error loading HWID";
            AppendLog($"Error loading HWID: {ex.Message}");
        }
    }

    private async void FlashButton_Click(object sender, RoutedEventArgs e)
    {
        if (DeviceTypeComboBox.SelectedItem is not System.Windows.Controls.ComboBoxItem selectedItem)
        {
            MessageBox.Show("Please select a device type", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var deviceType = selectedItem.Content.ToString();
        if (string.IsNullOrEmpty(deviceType))
        {
            return;
        }

        // Confirm with user
        var result = MessageBox.Show(
            $"This will flash firmware to a {deviceType} device.\n\n" +
            "Make sure:\n" +
            "1. Device is connected via USB\n" +
            "2. Necessary drivers are installed\n" +
            "3. Device is in flash mode\n\n" +
            "Continue?",
            "Confirm Flash",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        // Start flashing
        IsFlashing = true;
        StatusTextBlock.Text = "Flashing...";
        ServerStatusTextBlock.Text = "Connected";
        ServerStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
        ProgressTextBox.Clear();
        
        _cancellationTokenSource = new CancellationTokenSource();
        
        var progress = new Progress<string>(message =>
        {
            Dispatcher.Invoke(() =>
            {
                AppendLog(message);
            });
        });

        try
        {
            var success = await _flashClient.FlashDeviceAsync(
                deviceType,
                progress,
                _cancellationTokenSource.Token);

            if (success)
            {
                StatusTextBlock.Text = "Completed Successfully";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                
                MessageBox.Show(
                    "Firmware flash completed successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                StatusTextBlock.Text = "Failed";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                
                MessageBox.Show(
                    "Firmware flash failed. Check the log for details.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Cancelled";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
            AppendLog("Operation cancelled by user");
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Error";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            AppendLog($"Error: {ex.Message}");
            
            MessageBox.Show(
                $"An error occurred: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsFlashing = false;
            ServerStatusTextBlock.Text = "Disconnected";
            ServerStatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        AppendLog("Cancelling operation...");
    }

    private void AppendLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        ProgressTextBox.AppendText($"[{timestamp}] {message}\n");
        ProgressTextBox.ScrollToEnd();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
