using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Omnitooth.Core.Configuration;
using Omnitooth.Core.Interfaces;
using System.Reactive.Linq;

namespace Omnitooth.Application.Services;

/// <summary>
/// Integration service that orchestrates input capture, HID conversion, and Bluetooth transmission.
/// </summary>
public sealed class IntegrationService : BackgroundService
{
    private readonly ILogger<IntegrationService> _logger;
    private readonly IInputCaptureService _inputCaptureService;
    private readonly IHidService _hidService;
    private readonly IBluetoothService _bluetoothService;
    private readonly OmnitoothConfiguration _configuration;
    private readonly IDisposable _keyboardSubscription;
    private readonly IDisposable _mouseSubscription;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="inputCaptureService">The input capture service.</param>
    /// <param name="hidService">The HID service.</param>
    /// <param name="bluetoothService">The Bluetooth service.</param>
    /// <param name="configuration">The application configuration.</param>
    public IntegrationService(
        ILogger<IntegrationService> logger,
        IInputCaptureService inputCaptureService,
        IHidService hidService,
        IBluetoothService bluetoothService,
        IOptions<OmnitoothConfiguration> configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _inputCaptureService = inputCaptureService ?? throw new ArgumentNullException(nameof(inputCaptureService));
        _hidService = hidService ?? throw new ArgumentNullException(nameof(hidService));
        _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));

        // Subscribe to input events
        _keyboardSubscription = SubscribeToKeyboardInput();
        _mouseSubscription = SubscribeToMouseInput();
    }

    /// <summary>
    /// Gets a value indicating whether the integration service is running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Executes the integration service.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Integration service starting");

        try
        {
            IsRunning = true;

            // The service runs continuously, processing input events through subscriptions
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Integration service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Integration service encountered an error");
            throw;
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>
    /// Starts the input capture and Bluetooth services.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartServicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting integrated services");

            // Start Bluetooth service first
            await _bluetoothService.StartAsync(cancellationToken);
            _logger.LogInformation("Bluetooth service started");

            // Start input capture
            await _inputCaptureService.StartAsync(cancellationToken);
            _logger.LogInformation("Input capture started");

            _logger.LogInformation("All integrated services started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start integrated services");
            
            // Attempt cleanup on failure
            try
            {
                await StopServicesAsync(cancellationToken);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx, "Error during cleanup after failed start");
            }
            
            throw;
        }
    }

    /// <summary>
    /// Stops the input capture and Bluetooth services.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StopServicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Stopping integrated services");

            // Stop input capture first
            await _inputCaptureService.StopAsync(cancellationToken);
            _logger.LogInformation("Input capture stopped");

            // Stop Bluetooth service
            await _bluetoothService.StopAsync(cancellationToken);
            _logger.LogInformation("Bluetooth service stopped");

            _logger.LogInformation("All integrated services stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping integrated services");
            throw;
        }
    }

    /// <summary>
    /// Subscribes to keyboard input events and converts them to HID reports.
    /// </summary>
    /// <returns>The subscription disposable.</returns>
    private IDisposable SubscribeToKeyboardInput()
    {
        return _inputCaptureService.KeyboardInput
            .Where(input => ShouldProcessInput())
            .Select(input => _hidService.CreateKeyboardReport(input))
            .SelectMany(async report =>
            {
                try
                {
                    await _bluetoothService.SendReportAsync(report);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send keyboard HID report");
                    return false;
                }
            })
            .Subscribe(
                success =>
                {
                    if (success)
                    {
                        _logger.LogTrace("Keyboard HID report sent successfully");
                    }
                },
                error => _logger.LogError(error, "Error in keyboard input processing pipeline"));
    }

    /// <summary>
    /// Subscribes to mouse input events and converts them to HID reports.
    /// </summary>
    /// <returns>The subscription disposable.</returns>
    private IDisposable SubscribeToMouseInput()
    {
        return _inputCaptureService.MouseInput
            .Where(input => ShouldProcessInput())
            .Select(input => _hidService.CreateMouseReport(input))
            .SelectMany(async report =>
            {
                try
                {
                    await _bluetoothService.SendReportAsync(report);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send mouse HID report");
                    return false;
                }
            })
            .Subscribe(
                success =>
                {
                    if (success)
                    {
                        _logger.LogTrace("Mouse HID report sent successfully");
                    }
                },
                error => _logger.LogError(error, "Error in mouse input processing pipeline"));
    }

    /// <summary>
    /// Determines whether input should be processed based on current state.
    /// </summary>
    /// <returns>True if input should be processed; otherwise, false.</returns>
    private bool ShouldProcessInput()
    {
        // Only process input if all services are running and have connected devices
        return IsRunning &&
               _inputCaptureService.IsCapturing &&
               _bluetoothService.ServiceState == Core.Enums.ServiceState.Running &&
               _bluetoothService.ConnectedDevices.Count > 0;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public override void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogDebug("Disposing IntegrationService");

            try
            {
                _keyboardSubscription?.Dispose();
                _mouseSubscription?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during IntegrationService disposal");
            }
            finally
            {
                _disposed = true;
                _logger.LogDebug("IntegrationService disposed");
            }
        }

        base.Dispose();
    }
}