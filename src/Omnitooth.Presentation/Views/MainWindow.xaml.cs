using Microsoft.Extensions.Logging;
using ModernWpf.Controls;
using Omnitooth.Application.Services;
using Omnitooth.Core.Interfaces;
using System.Windows;
using System.Windows.Media;

namespace Omnitooth.Presentation.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly IntegrationService _integrationService;
    private readonly IBluetoothService _bluetoothService;
    private bool _isServiceRunning = false;
    private IDisposable? _deviceConnectionSubscription;
    private IDisposable? _deviceDisconnectionSubscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="integrationService">The integration service.</param>
    /// <param name="bluetoothService">The Bluetooth service.</param>
    public MainWindow(ILogger<MainWindow> logger, IntegrationService integrationService, IBluetoothService bluetoothService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _integrationService = integrationService ?? throw new ArgumentNullException(nameof(integrationService));
        _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
        
        InitializeComponent();
        InitializeWindow();
        SetupServiceSubscriptions();
    }

    /// <summary>
    /// Initializes the window.
    /// </summary>
    private void InitializeWindow()
    {
        // Set version info
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"v{version?.ToString(3) ?? "1.0.0"}";

        // Initialize status
        UpdateServiceStatus(false);
        UpdateStatusIndicator("Ready", Colors.Gray);

        _logger.LogInformation("Main window initialized");
    }

    /// <summary>
    /// Handles the start/stop button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void StartStop_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isServiceRunning)
            {
                await StopServiceAsync();
            }
            else
            {
                await StartServiceAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling service state");
            AddLogMessage($"Error: {ex.Message}");
            await ShowErrorDialog("Service Error", $"Failed to {(_isServiceRunning ? "stop" : "start")} service: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts the Bluetooth HID service.
    /// </summary>
    private async Task StartServiceAsync()
    {
        AddLogMessage("Starting Bluetooth HID service...");
        UpdateStatusIndicator("Starting service...", Colors.Orange);

        try
        {
            await _integrationService.StartServicesAsync();
            
            _isServiceRunning = true;
            UpdateServiceStatus(true);
            UpdateStatusIndicator("Service running - Ready for connections", Colors.Green);
            AddLogMessage("Bluetooth HID service started successfully");
            AddLogMessage($"Device name: {_bluetoothService.IsAdvertising}");
            
            _logger.LogInformation("Bluetooth HID service started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Bluetooth HID service");
            UpdateStatusIndicator("Failed to start service", Colors.Red);
            AddLogMessage($"Failed to start service: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Stops the Bluetooth HID service.
    /// </summary>
    private async Task StopServiceAsync()
    {
        AddLogMessage("Stopping Bluetooth HID service...");
        UpdateStatusIndicator("Stopping service...", Colors.Orange);

        try
        {
            await _integrationService.StopServicesAsync();
            
            _isServiceRunning = false;
            UpdateServiceStatus(false);
            UpdateStatusIndicator("Service stopped", Colors.Gray);
            AddLogMessage("Bluetooth HID service stopped");
            
            // Clear connected devices list
            Dispatcher.Invoke(() =>
            {
                ConnectedDevicesList.Items.Clear();
                NoDevicesText.Visibility = Visibility.Visible;
                ConnectedDevicesList.Visibility = Visibility.Collapsed;
            });
            
            _logger.LogInformation("Bluetooth HID service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop Bluetooth HID service");
            AddLogMessage($"Error stopping service: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates the service status display.
    /// </summary>
    /// <param name="isRunning">Whether the service is running.</param>
    private void UpdateServiceStatus(bool isRunning)
    {
        _isServiceRunning = isRunning;
        ServiceStatusText.Text = isRunning ? "Running" : "Stopped";
        StartStopButton.Content = isRunning ? "Stop Service" : "Start Service";
    }

    /// <summary>
    /// Updates the status indicator.
    /// </summary>
    /// <param name="status">The status text.</param>
    /// <param name="color">The indicator color.</param>
    private void UpdateStatusIndicator(string status, Color color)
    {
        StatusText.Text = status;
        StatusIndicator.Fill = new SolidColorBrush(color);
    }

    /// <summary>
    /// Adds a message to the activity log.
    /// </summary>
    /// <param name="message">The message to add.</param>
    private void AddLogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}";
        
        Dispatcher.Invoke(() =>
        {
            if (LogTextBlock.Text == "Application started. Waiting for service to start...")
            {
                LogTextBlock.Text = logEntry;
            }
            else
            {
                LogTextBlock.Text += Environment.NewLine + logEntry;
            }

            if (AutoScrollCheckBox.IsChecked == true)
            {
                LogScrollViewer.ScrollToEnd();
            }
        });
    }

    /// <summary>
    /// Handles the clear log button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogTextBlock.Text = string.Empty;
        AddLogMessage("Log cleared");
    }

    /// <summary>
    /// Handles the settings button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement settings dialog
        await ShowInfoDialog("Settings", "Settings dialog not yet implemented.");
    }

    /// <summary>
    /// Handles the about button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void About_Click(object sender, RoutedEventArgs e)
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var aboutText = $"Omnitooth - Bluetooth HID Emulator\n\n" +
                       $"Version: {version?.ToString() ?? "1.0.0.0"}\n" +
                       $"Built with .NET 8 and WPF\n\n" +
                       $"A modern Bluetooth Low Energy HID device emulator\n" +
                       $"that allows your Windows PC to act as a wireless\n" +
                       $"keyboard and mouse for other devices.";

        await ShowInfoDialog("About Omnitooth", aboutText);
    }

    /// <summary>
    /// Shows an information dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    private async Task ShowInfoDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK"
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows an error dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The error message.</param>
    private async Task ShowErrorDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK"
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Sets up subscriptions to Bluetooth service events.
    /// </summary>
    private void SetupServiceSubscriptions()
    {
        // Subscribe to device connection events
        _deviceConnectionSubscription = _bluetoothService.DeviceConnected.Subscribe(device =>
        {
            Dispatcher.Invoke(() =>
            {
                AddLogMessage($"Device connected: {device.Name} ({device.Address})");
                UpdateConnectedDevicesList();
            });
        });

        // Subscribe to device disconnection events
        _deviceDisconnectionSubscription = _bluetoothService.DeviceDisconnected.Subscribe(device =>
        {
            Dispatcher.Invoke(() =>
            {
                AddLogMessage($"Device disconnected: {device.Name} ({device.Address})");
                UpdateConnectedDevicesList();
            });
        });
    }

    /// <summary>
    /// Updates the connected devices list in the UI.
    /// </summary>
    private void UpdateConnectedDevicesList()
    {
        var connectedDevices = _bluetoothService.ConnectedDevices;
        
        ConnectedDevicesList.Items.Clear();
        
        if (connectedDevices.Count > 0)
        {
            foreach (var device in connectedDevices)
            {
                var deviceInfo = $"{device.Name} - {device.Address} ({device.ConnectionState})";
                ConnectedDevicesList.Items.Add(deviceInfo);
            }
            
            NoDevicesText.Visibility = Visibility.Collapsed;
            ConnectedDevicesList.Visibility = Visibility.Visible;
        }
        else
        {
            NoDevicesText.Visibility = Visibility.Visible;
            ConnectedDevicesList.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Handles window closing to clean up resources.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        try
        {
            _deviceConnectionSubscription?.Dispose();
            _deviceDisconnectionSubscription?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing subscriptions");
        }
        
        base.OnClosed(e);
    }
}