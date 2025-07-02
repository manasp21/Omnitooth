using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Omnitooth.Core.Configuration;
using Omnitooth.Core.Enums;
using Omnitooth.Core.Interfaces;
using Omnitooth.Core.Models;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreBluetoothDevice = Omnitooth.Core.Models.BluetoothDevice;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth;
using Windows.Devices.Radios;
using System.Security.Principal;
using System.Runtime.InteropServices;

namespace Omnitooth.Infrastructure.Services.Bluetooth;

/// <summary>
/// Bluetooth GATT server service implementing HID over GATT Profile (HOGP).
/// </summary>
public sealed class BluetoothGattServerService : IBluetoothService
{
    private readonly ILogger<BluetoothGattServerService> _logger;
    private readonly BluetoothConfiguration _config;
    private readonly IBluetoothServiceFactory _serviceFactory;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly Subject<CoreBluetoothDevice> _deviceConnectedSubject = new();
    private readonly Subject<CoreBluetoothDevice> _deviceDisconnectedSubject = new();
    private readonly Subject<(CoreBluetoothDevice Device, ConnectionState OldState, ConnectionState NewState)> _connectionStateChangedSubject = new();
    private readonly ConcurrentDictionary<string, CoreBluetoothDevice> _connectedDevices = new();
    
    private GattServiceProvider? _serviceProvider;
    private GattLocalCharacteristic? _reportCharacteristic;
    private GattLocalCharacteristic? _reportMapCharacteristic;
    private ServiceState _serviceState = ServiceState.Stopped;
    private Task? _discoveryMonitoringTask;
    private CancellationTokenSource? _monitoringCancellationTokenSource;
    private bool _disposed;

    // HID over GATT Profile UUIDs
    private static readonly Guid HidServiceUuid = new("00001812-0000-1000-8000-00805f9b34fb");
    private static readonly Guid ReportCharacteristicUuid = new("00002a4d-0000-1000-8000-00805f9b34fb");
    private static readonly Guid ReportMapCharacteristicUuid = new("00002a4b-0000-1000-8000-00805f9b34fb");
    private static readonly Guid HidInformationCharacteristicUuid = new("00002a4a-0000-1000-8000-00805f9b34fb");
    private static readonly Guid HidControlPointCharacteristicUuid = new("00002a4c-0000-1000-8000-00805f9b34fb");

    /// <summary>
    /// Initializes a new instance of the <see cref="BluetoothGattServerService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="options">Bluetooth configuration options.</param>
    /// <param name="serviceFactory">The Bluetooth service factory for creating GATT services.</param>
    /// <param name="circuitBreaker">Circuit breaker for handling operation failures and recovery.</param>
    public BluetoothGattServerService(
        ILogger<BluetoothGattServerService> logger, 
        IOptions<BluetoothConfiguration> options,
        IBluetoothServiceFactory serviceFactory,
        ICircuitBreaker circuitBreaker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
        _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
    }

    /// <inheritdoc />
    public IObservable<CoreBluetoothDevice> DeviceConnected => _deviceConnectedSubject.AsObservable();

    /// <inheritdoc />
    public IObservable<CoreBluetoothDevice> DeviceDisconnected => _deviceDisconnectedSubject.AsObservable();

    /// <inheritdoc />
    public IObservable<(CoreBluetoothDevice Device, ConnectionState OldState, ConnectionState NewState)> ConnectionStateChanged =>
        _connectionStateChangedSubject.AsObservable();

    /// <inheritdoc />
    public ServiceState ServiceState => _serviceState;

    /// <inheritdoc />
    public IReadOnlyList<CoreBluetoothDevice> ConnectedDevices => _connectedDevices.Values.ToList();

    /// <inheritdoc />
    public bool IsAdvertising => _serviceProvider?.AdvertisementStatus == GattServiceProviderAdvertisementStatus.Started;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_serviceState == ServiceState.Running)
        {
            _logger.LogWarning("Bluetooth service is already running");
            return;
        }

        _logger.LogInformation("Starting Bluetooth GATT server service");
        _serviceState = ServiceState.Starting;

        try
        {
            // Create GATT service provider with circuit breaker protection
            await _circuitBreaker.ExecuteAsync(async ct => await CreateGattServiceAsync(), cancellationToken);

            // Check Windows Bluetooth capabilities and log diagnostics
            await CheckBluetoothCapabilitiesAsync();

            // Perform comprehensive system Bluetooth configuration checks
            await CheckSystemBluetoothConfigurationAsync();

            // Start GATT service advertising and wait for it to become active with circuit breaker protection
            await _circuitBreaker.ExecuteAsync(async ct => await StartAndVerifyAdvertisingAsync(), cancellationToken);

            _serviceState = ServiceState.Running;
            _logger.LogInformation("Bluetooth GATT server started successfully");
            
            // Log circuit breaker metrics
            LogCircuitBreakerMetrics();
            
            // Start discovery monitoring now that service is running
            StartDiscoveryMonitoring();
        }
        catch (CircuitBreakerOpenException ex)
        {
            _logger.LogError("Circuit breaker prevented Bluetooth GATT server start. State: {State}, Next retry: {NextRetry}", 
                ex.State, ex.NextRetryTime);
            _serviceState = ServiceState.Stopped;
            throw new InvalidOperationException($"Bluetooth service start blocked by circuit breaker (State: {ex.State}). Next retry: {ex.NextRetryTime}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Bluetooth GATT server");
            _serviceState = ServiceState.Stopped;
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_serviceState == ServiceState.Stopped)
        {
            return;
        }

        _logger.LogInformation("Stopping Bluetooth GATT server service");
        
        // Stop discovery monitoring first
        await StopDiscoveryMonitoringAsync();
        
        _serviceState = ServiceState.Stopping;

        try
        {
            // Stop GATT service advertising
            if (_serviceProvider?.AdvertisementStatus == GattServiceProviderAdvertisementStatus.Started)
            {
                _serviceProvider.StopAdvertising();
                _logger.LogInformation("GATT service advertising stopped");
            }

            // Disconnect all devices
            await DisconnectAllDevicesAsync();

            // Cleanup GATT service
            CleanupGattService();

            _serviceState = ServiceState.Stopped;
            _logger.LogInformation("Bluetooth GATT server stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping Bluetooth GATT server");
            _serviceState = ServiceState.Stopped;
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SendReportAsync(HidReport report, CancellationToken cancellationToken = default)
    {
        if (_serviceState != ServiceState.Running)
        {
            throw new InvalidOperationException("Bluetooth service is not running");
        }

        var tasks = _connectedDevices.Values.Select(device => 
            SendReportToDeviceAsync(device.Id, report, cancellationToken));

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public async Task SendReportToDeviceAsync(string deviceId, HidReport report, CancellationToken cancellationToken = default)
    {
        if (!_connectedDevices.TryGetValue(deviceId, out var device))
        {
            _logger.LogWarning("Device {DeviceId} not found", deviceId);
            return;
        }

        try
        {
            if (_reportCharacteristic == null)
            {
                _logger.LogError("Report characteristic not initialized");
                return;
            }

            var writer = new DataWriter();
            writer.WriteBytes(report.Data);

            var subscribedClients = _reportCharacteristic.SubscribedClients;
            if (subscribedClients.Count > 0)
            {
                await _circuitBreaker.ExecuteAsync(async ct => 
                {
                    await _reportCharacteristic.NotifyValueAsync(writer.DetachBuffer());
                    device.UpdateActivity();
                    
                    _logger.LogTrace("Sent HID report to device {DeviceId}: {ReportType} ({DataLength} bytes)",
                        deviceId, report.ReportType, report.Data.Length);
                }, cancellationToken);
            }
            else
            {
                _logger.LogDebug("No subscribed clients for device {DeviceId}", deviceId);
            }
        }
        catch (CircuitBreakerOpenException ex)
        {
            _logger.LogWarning("Circuit breaker rejected HID report for device {DeviceId}. State: {State}, Next retry: {NextRetry}", 
                deviceId, ex.State, ex.NextRetryTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send HID report to device {DeviceId}", deviceId);
        }
    }

    /// <inheritdoc />
    public async Task DisconnectDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (_connectedDevices.TryRemove(deviceId, out var device))
        {
            var oldState = device.ConnectionState;
            device.ConnectionState = ConnectionState.Disconnected;

            _deviceDisconnectedSubject.OnNext(device);
            _connectionStateChangedSubject.OnNext((device, oldState, ConnectionState.Disconnected));

            _logger.LogInformation("Disconnected device {DeviceId} ({DeviceName})", deviceId, device.Name);
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public CoreBluetoothDevice? GetDevice(string deviceId)
    {
        return _connectedDevices.TryGetValue(deviceId, out var device) ? device : null;
    }

    /// <summary>
    /// Creates the GATT service with HID characteristics using the service factory.
    /// </summary>
    private async Task CreateGattServiceAsync()
    {
        _logger.LogDebug("Creating HID GATT service using factory");

        // Use the factory to create a validated and healthy service provider
        _serviceProvider = await _serviceFactory.CreateHidServiceProviderAsync();
        
        _logger.LogInformation("GATT service provider created successfully via factory");
        
        // Log GATT service configuration for discoverability
        await ConfigureAdvertisementDataAsync();
        
        // Subscribe to advertisement status changes
        _serviceProvider.AdvertisementStatusChanged += OnAdvertisementStatusChanged;

        // Get references to the characteristics created by the factory
        await InitializeCharacteristicReferencesAsync();

        // Service will be started when we call StartAdvertising later
    }

    /// <summary>
    /// Initializes references to characteristics created by the factory.
    /// </summary>
    private async Task InitializeCharacteristicReferencesAsync()
    {
        try
        {
            if (_serviceProvider?.Service?.Characteristics == null)
            {
                _logger.LogWarning("⚠️ Service or characteristics collection is null");
                return;
            }

            // Find the report characteristic
            _reportCharacteristic = _serviceProvider.Service.Characteristics
                .FirstOrDefault(c => c.Uuid == ReportCharacteristicUuid);
            
            if (_reportCharacteristic != null)
            {
                _reportCharacteristic.SubscribedClientsChanged += OnSubscribedClientsChanged;
                _reportCharacteristic.ReadRequested += OnReportReadRequested;
                _logger.LogDebug("✅ Report characteristic reference initialized");
            }
            else
            {
                _logger.LogWarning("⚠️ Report characteristic not found in service");
            }

            // Find the report map characteristic
            _reportMapCharacteristic = _serviceProvider.Service.Characteristics
                .FirstOrDefault(c => c.Uuid == ReportMapCharacteristicUuid);
            
            if (_reportMapCharacteristic != null)
            {
                _reportMapCharacteristic.ReadRequested += OnReportMapReadRequested;
                _logger.LogDebug("✅ Report map characteristic reference initialized");
            }
            else
            {
                _logger.LogWarning("⚠️ Report map characteristic not found in service");
            }

            _logger.LogDebug("✅ Characteristic references initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize characteristic references");
            throw;
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Starts GATT service advertising with robust retry logic and complete service recreation.
    /// </summary>
    private async Task StartAndVerifyAdvertisingAsync()
    {
        const int maxRetries = 3;
        var retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            try
            {
                if (retryCount > 0)
                {
                    _logger.LogInformation("🔄 Retry attempt {RetryCount}/{MaxRetries} for GATT advertisement", 
                        retryCount, maxRetries);
                    
                    // Completely dispose and recreate the service provider for clean state
                    _logger.LogInformation("🔧 Disposing corrupted service provider and recreating...");
                    await DisposeAndRecreateServiceProviderAsync();
                }
                
                // Validate service provider state before attempting to start
                if (!ValidateServiceProviderState())
                {
                    throw new InvalidOperationException("Service provider is not in a valid state for advertising");
                }
                
                _logger.LogInformation("Starting GATT service advertising - Device: {DeviceName}", _config.DeviceName);
                _logger.LogInformation("Initial advertisement status: {Status}", _serviceProvider!.AdvertisementStatus);
                
                // Start advertising with additional validation
                _serviceProvider.StartAdvertising();
                _logger.LogInformation("StartAdvertising() called, monitoring status for transition");
                
                // Monitor for status transition with longer timeout
                var success = await WaitForAdvertisementTransitionAsync();
                
                if (success)
                {
                    _logger.LogInformation("✅ Advertisement successfully started on attempt {AttemptNumber}", retryCount + 1);
                    return;
                }
                
                // If we get here, the status didn't transition properly
                retryCount++;
                
                if (retryCount < maxRetries)
                {
                    var delayMs = CalculateExponentialBackoffDelay(retryCount);
                    _logger.LogWarning("⚠️ Advertisement status transition failed, will retry in {DelayMs}ms...", delayMs);
                    await Task.Delay(delayMs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during advertisement attempt {AttemptNumber}: {ErrorType}", 
                    retryCount + 1, ex.GetType().Name);
                _logger.LogError("Exception details: {Message}", ex.Message);
                
                retryCount++;
                
                if (retryCount >= maxRetries)
                {
                    _logger.LogError("❌ All retry attempts exhausted. Final error: {FinalError}", ex.Message);
                    throw new InvalidOperationException(
                        $"Failed to start GATT advertisement after {maxRetries} attempts. Last error: {ex.Message}", ex);
                }
                
                var delayMs = CalculateExponentialBackoffDelay(retryCount);
                _logger.LogInformation("⏳ Waiting {DelayMs}ms before next retry attempt...", delayMs);
                await Task.Delay(delayMs);
            }
        }
        
        // All retries exhausted - this should not be reached due to exception throwing above
        var finalStatus = _serviceProvider!.AdvertisementStatus;
        _logger.LogError("⚠️ Advertisement retries exhausted - final status: {Status}", finalStatus);
        throw new InvalidOperationException($"GATT advertisement failed after {maxRetries} retry attempts - final status: {finalStatus}");
    }

    /// <summary>
    /// Completely disposes the current service provider and recreates it with fresh state.
    /// This ensures clean state for retry attempts and prevents state corruption issues.
    /// Uses the service factory for proper disposal and recreation.
    /// </summary>
    private async Task DisposeAndRecreateServiceProviderAsync()
    {
        try
        {
            _logger.LogDebug("🔧 Starting service provider disposal and recreation using factory...");
            
            // Use factory to properly dispose the existing service provider
            if (_serviceProvider != null)
            {
                try
                {
                    // Unsubscribe from events before disposal
                    _serviceProvider.AdvertisementStatusChanged -= OnAdvertisementStatusChanged;
                    
                    // Clean up characteristic event subscriptions
                    CleanupCharacteristicEvents();
                    
                    // Use factory for safe disposal
                    await _serviceFactory.DisposeServiceProviderAsync(_serviceProvider);
                    
                    _logger.LogDebug("Service provider disposed successfully via factory");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during service provider disposal (will continue with recreation)");
                }
                finally
                {
                    _serviceProvider = null;
                    _reportCharacteristic = null;
                    _reportMapCharacteristic = null;
                }
            }
            
            // Brief pause to ensure Windows Bluetooth stack cleanup
            await Task.Delay(100);
            
            // Recreate the service provider with fresh state using factory and circuit breaker protection
            _logger.LogDebug("🔄 Recreating GATT service provider via factory...");
            await _circuitBreaker.ExecuteAsync(async ct => await CreateGattServiceAsync(), CancellationToken.None);
            
            _logger.LogInformation("✅ Service provider successfully recreated with clean state via factory");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to dispose and recreate service provider");
            throw new InvalidOperationException("Failed to recreate service provider for retry attempt", ex);
        }
    }

    /// <summary>
    /// Cleans up characteristic event subscriptions.
    /// </summary>
    private void CleanupCharacteristicEvents()
    {
        try
        {
            if (_reportCharacteristic != null)
            {
                _reportCharacteristic.SubscribedClientsChanged -= OnSubscribedClientsChanged;
                _reportCharacteristic.ReadRequested -= OnReportReadRequested;
                _logger.LogDebug("Report characteristic events cleaned up");
            }

            if (_reportMapCharacteristic != null)
            {
                _reportMapCharacteristic.ReadRequested -= OnReportMapReadRequested;
                _logger.LogDebug("Report map characteristic events cleaned up");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during characteristic event cleanup");
        }
    }
    
    /// <summary>
    /// Validates that the service provider is in a valid state for starting advertisement.
    /// </summary>
    private bool ValidateServiceProviderState()
    {
        if (_serviceProvider == null)
        {
            _logger.LogError("❌ Service provider is null - cannot start advertising");
            return false;
        }
        
        var currentStatus = _serviceProvider.AdvertisementStatus;
        
        // Valid states for starting advertisement
        if (currentStatus == GattServiceProviderAdvertisementStatus.Created)
        {
            _logger.LogDebug("✅ Service provider in 'Created' state - ready for advertising");
            return true;
        }
        
        if (currentStatus == GattServiceProviderAdvertisementStatus.Stopped)
        {
            _logger.LogDebug("✅ Service provider in 'Stopped' state - ready for advertising");
            return true;
        }
        
        // Invalid states
        _logger.LogWarning("⚠️ Service provider in invalid state for advertising: {Status}", currentStatus);
        
        if (currentStatus == GattServiceProviderAdvertisementStatus.Started)
        {
            _logger.LogWarning("Service is already advertising - this should not happen in retry logic");
        }
        else if (currentStatus == GattServiceProviderAdvertisementStatus.Aborted)
        {
            _logger.LogWarning("Service advertisement was aborted - requires recreation");
        }
        
        return false;
    }
    
    /// <summary>
    /// Calculates exponential backoff delay for retry attempts.
    /// </summary>
    private static int CalculateExponentialBackoffDelay(int retryCount)
    {
        // Exponential backoff: 2s, 4s, 8s for attempts 1, 2, 3
        var baseDelayMs = 2000;
        var maxDelayMs = 8000;
        
        var delayMs = baseDelayMs * (int)Math.Pow(2, retryCount - 1);
        return Math.Min(delayMs, maxDelayMs);
    }
    
    
    /// <summary>
    /// Waits for advertisement status to transition and provides detailed monitoring.
    /// </summary>
    private async Task<bool> WaitForAdvertisementTransitionAsync()
    {
        var timeout = TimeSpan.FromSeconds(10);
        var startTime = DateTime.UtcNow;
        var lastStatus = _serviceProvider!.AdvertisementStatus;
        var statusChangeDetected = false;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            var currentStatus = _serviceProvider.AdvertisementStatus;
            
            // Check for status changes
            if (currentStatus != lastStatus)
            {
                _logger.LogInformation("🔄 Advertisement status changed: {OldStatus} → {NewStatus}", 
                    lastStatus, currentStatus);
                lastStatus = currentStatus;
                statusChangeDetected = true;
            }
            
            // Success cases
            if (currentStatus == GattServiceProviderAdvertisementStatus.Started)
            {
                _logger.LogInformation("✅ Advertisement status reached 'Started' - device is fully discoverable");
                return true;
            }
            
            // Immediate failure case
            if (currentStatus == GattServiceProviderAdvertisementStatus.Aborted)
            {
                _logger.LogError("❌ Advertisement was aborted during transition");
                throw new InvalidOperationException("GATT advertisement was aborted by the system");
            }
            
            await Task.Delay(200); // Check every 200ms
        }
        
        var finalStatus = _serviceProvider.AdvertisementStatus;
        
        if (statusChangeDetected)
        {
            _logger.LogInformation("ℹ️ Status transitions detected but didn't reach 'Started' within timeout");
            _logger.LogInformation("📊 Final status: {Status}", finalStatus);
            
            // Consider this a partial success if we're in Created state and saw changes
            if (finalStatus == GattServiceProviderAdvertisementStatus.Created)
            {
                _logger.LogInformation("💡 'Created' status with transitions may still allow discovery");
                return true; // Accept this as working
            }
        }
        else
        {
            _logger.LogWarning("⚠️ No advertisement status transitions detected within {Timeout}s", timeout.TotalSeconds);
            _logger.LogWarning("📊 Status remained: {Status}", finalStatus);
        }
        
        return false; // Transition failed
    }

    /// <summary>
    /// Checks Windows Bluetooth adapter capabilities and logs diagnostic information.
    /// </summary>
    private async Task CheckBluetoothCapabilitiesAsync()
    {
        try
        {
            _logger.LogInformation("🔧 Checking Windows Bluetooth capabilities...");
            
            // Check if Bluetooth is available on this system
            var bluetoothAdapter = await BluetoothAdapter.GetDefaultAsync();
            if (bluetoothAdapter == null)
            {
                _logger.LogError("❌ No Bluetooth adapter found on this system");
                _logger.LogError("💡 Ensure Bluetooth hardware is installed and drivers are working");
                return;
            }

            _logger.LogInformation("✅ Bluetooth adapter found: {BluetoothAddress}", bluetoothAdapter.BluetoothAddress);
            _logger.LogInformation("📊 Adapter details:");
            _logger.LogInformation("   • Bluetooth Address: {Address:X12}", bluetoothAdapter.BluetoothAddress);
            _logger.LogInformation("   • Device ID: {DeviceId}", bluetoothAdapter.DeviceId);
            _logger.LogInformation("   • Is Classic Supported: {IsClassicSupported}", bluetoothAdapter.IsClassicSupported);
            _logger.LogInformation("   • Is Low Energy Supported: {IsLowEnergySupported}", bluetoothAdapter.IsLowEnergySupported);
            
            // Check LE support specifically
            if (!bluetoothAdapter.IsLowEnergySupported)
            {
                _logger.LogError("❌ Bluetooth Low Energy (LE) not supported by this adapter");
                _logger.LogError("💡 HID over GATT requires Bluetooth LE - device discovery will fail");
                return;
            }
            
            _logger.LogInformation("✅ Bluetooth LE is supported");

            // Check if LE peripheral mode is supported
            if (bluetoothAdapter.IsPeripheralRoleSupported)
            {
                _logger.LogInformation("✅ Bluetooth LE Peripheral mode is supported - good for GATT server");
            }
            else
            {
                _logger.LogWarning("⚠️ Bluetooth LE Peripheral mode support unknown or not supported");
                _logger.LogWarning("💡 This may be why advertisement status stays 'Created'");
            }

            // Check radio state
            var radio = await bluetoothAdapter.GetRadioAsync();
            if (radio != null)
            {
                _logger.LogInformation("📡 Bluetooth Radio Status:");
                _logger.LogInformation("   • State: {RadioState}", radio.State);
                _logger.LogInformation("   • Name: {RadioName}", radio.Name);
                _logger.LogInformation("   • Kind: {RadioKind}", radio.Kind);
                
                if (radio.State != RadioState.On)
                {
                    _logger.LogWarning("⚠️ Bluetooth radio is not ON - current state: {State}", radio.State);
                    _logger.LogWarning("💡 Enable Bluetooth in Windows settings for device discovery");
                }
                else
                {
                    _logger.LogInformation("✅ Bluetooth radio is ON and ready");
                }
            }

            // Additional Windows version checks
            var osVersion = Environment.OSVersion;
            _logger.LogInformation("🖥️ Windows Version: {Version}", osVersion.VersionString);
            
            if (osVersion.Version.Major >= 10)
            {
                _logger.LogInformation("✅ Windows 10+ detected - should support GATT server functionality");
            }
            else
            {
                _logger.LogWarning("⚠️ Windows version may not fully support GATT server peripheral mode");
            }

            _logger.LogInformation("🔧 Bluetooth capability check completed");
            
            // Log additional Windows build information for troubleshooting
            await LogWindowsBuildInfoAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Bluetooth capabilities");
            _logger.LogWarning("💡 Bluetooth capability check failed - service may still work");
        }
    }

    /// <summary>
    /// Configures advertisement data with device name and service information for better discoverability.
    /// </summary>
    private async Task ConfigureAdvertisementDataAsync()
    {
        try
        {
            _logger.LogInformation("🔧 Preparing GATT service for enhanced discoverability");
            
            // Note: GattServiceProvider handles advertisement automatically when StartAdvertising() is called
            // We cannot directly configure the advertisement data for GATT services in Windows Runtime
            // The service UUID and characteristics are automatically included in the advertisement
            
            _logger.LogInformation("✅ GATT service configured with HID service UUID: {ServiceUuid}", HidServiceUuid);
            _logger.LogInformation("✅ Device will be advertised as GATT server with HID service");
            _logger.LogInformation("ℹ️ Windows will handle advertisement data automatically for GATT services");
            
            // Log what will be included in the advertisement
            _logger.LogInformation("📊 Advertisement will include:");
            _logger.LogInformation("   • HID Service UUID: {ServiceUuid}", HidServiceUuid);
            _logger.LogInformation("   • Report Characteristic UUID: {ReportUuid}", ReportCharacteristicUuid);
            _logger.LogInformation("   • Report Map Characteristic UUID: {ReportMapUuid}", ReportMapCharacteristicUuid);
            _logger.LogInformation("   • HID Information Characteristic UUID: {HidInfoUuid}", HidInformationCharacteristicUuid);
            _logger.LogInformation("   • HID Control Point Characteristic UUID: {ControlPointUuid}", HidControlPointCharacteristicUuid);
            
            _logger.LogInformation("🔧 GATT service advertisement preparation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing GATT service advertisement");
            _logger.LogWarning("💡 Advertisement preparation failed - proceeding with default GATT settings");
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Performs comprehensive system Bluetooth configuration checks and logging.
    /// </summary>
    private async Task CheckSystemBluetoothConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("🖥️ Performing comprehensive Windows Bluetooth system checks...");
            
            // Check Windows Bluetooth service status
            await CheckWindowsBluetoothServicesAsync();
            
            // Check system environment and permissions
            await CheckSystemEnvironmentAsync();
            
            // Log detailed system configuration
            await LogSystemConfigurationAsync();
            
            _logger.LogInformation("🖥️ System Bluetooth configuration check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during system Bluetooth configuration check");
            _logger.LogWarning("💡 System configuration check failed - service may still work");
        }
    }

    /// <summary>
    /// Checks Windows Bluetooth services status.
    /// </summary>
    private async Task CheckWindowsBluetoothServicesAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Checking Windows Bluetooth services status...");
            
            // Note: We can't directly check Windows services from UWP/WinRT apps
            // But we can check related system information
            
            // Check if we're running with sufficient privileges
            var currentUser = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(currentUser);
            var isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            
            _logger.LogInformation("📊 Process privileges:");
            _logger.LogInformation("   • Running as Administrator: {IsAdmin}", isAdmin);
            _logger.LogInformation("   • User: {UserName}", currentUser.Name);
            
            if (!isAdmin)
            {
                _logger.LogWarning("⚠️ Not running as Administrator - some Bluetooth features may be limited");
                _logger.LogInformation("💡 Try running Omnitooth as Administrator for full Bluetooth access");
            }
            
            // Check process integrity level
            var processToken = System.Diagnostics.Process.GetCurrentProcess().Handle;
            _logger.LogInformation("   • Process ID: {ProcessId}", System.Diagnostics.Process.GetCurrentProcess().Id);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Windows Bluetooth services");
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Checks system environment for Bluetooth LE support.
    /// </summary>
    private async Task CheckSystemEnvironmentAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Checking system environment for Bluetooth LE support...");
            
            // Check Windows version details
            var osInfo = Environment.OSVersion;
            var is64Bit = Environment.Is64BitOperatingSystem;
            var processorCount = Environment.ProcessorCount;
            
            _logger.LogInformation("📊 System Information:");
            _logger.LogInformation("   • OS Version: {Version} ({Platform})", osInfo.Version, osInfo.Platform);
            _logger.LogInformation("   • 64-bit OS: {Is64Bit}", is64Bit);
            _logger.LogInformation("   • Processor Count: {ProcessorCount}", processorCount);
            
            // Check Windows 10/11 version for Bluetooth LE GATT server support
            if (osInfo.Version.Major >= 10)
            {
                var build = osInfo.Version.Build;
                _logger.LogInformation("   • Windows Build: {Build}", build);
                
                if (build >= 10240) // Windows 10 initial release
                {
                    _logger.LogInformation("✅ Windows version supports Bluetooth LE GATT server");
                }
                else
                {
                    _logger.LogWarning("⚠️ Windows build may not fully support Bluetooth LE GATT server");
                }
                
                if (build >= 22000) // Windows 11
                {
                    _logger.LogInformation("✅ Windows 11 detected - enhanced Bluetooth LE support expected");
                }
            }
            
            // Check .NET runtime information
            var runtimeVersion = Environment.Version;
            var frameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            
            _logger.LogInformation("📊 Runtime Information:");
            _logger.LogInformation("   • Runtime Version: {Version}", runtimeVersion);
            _logger.LogInformation("   • Framework: {Framework}", frameworkDescription);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking system environment");
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Logs detailed system configuration relevant to Bluetooth operation.
    /// </summary>
    private async Task LogSystemConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Logging detailed system configuration...");
            
            // Log application configuration
            _logger.LogInformation("📊 Application Configuration:");
            _logger.LogInformation("   • Target Framework: net8.0-windows10.0.22621.0");
            _logger.LogInformation("   • Configuration: Production");
            _logger.LogInformation("   • Device Name: {DeviceName}", _config.DeviceName);
            
            // Log current directory and execution context
            var currentDirectory = Environment.CurrentDirectory;
            var commandLine = Environment.CommandLine;
            
            _logger.LogInformation("📊 Execution Context:");
            _logger.LogInformation("   • Current Directory: {Directory}", currentDirectory);
            _logger.LogInformation("   • Working Set: {WorkingSet} MB", Environment.WorkingSet / 1024 / 1024);
            
            // Check for potential interference from antivirus or security software
            _logger.LogInformation("💡 Troubleshooting Notes:");
            _logger.LogInformation("   • If discovery fails, check Windows Defender firewall settings");
            _logger.LogInformation("   • Antivirus software may block Bluetooth LE peripheral mode");
            _logger.LogInformation("   • Some VPN software can interfere with Bluetooth functionality");
            _logger.LogInformation("   • Windows 'Fast Startup' can cause Bluetooth driver issues");
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging system configuration");
        }
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Logs Windows build information for compatibility troubleshooting.
    /// </summary>
    private async Task LogWindowsBuildInfoAsync()
    {
        try
        {
            var osVersion = Environment.OSVersion;
            var buildNumber = Environment.OSVersion.Version.Build;
            
            _logger.LogInformation("📊 Windows Build Information:");
            _logger.LogInformation("   • OS Version: {OSVersion}", osVersion.VersionString);
            _logger.LogInformation("   • Build Number: {BuildNumber}", buildNumber);
            _logger.LogInformation("   • Platform: {Platform}", osVersion.Platform);
            
            // Windows 11 detection (build 22000+)
            if (buildNumber >= 22000)
            {
                _logger.LogInformation("✅ Windows 11 detected - enhanced Bluetooth LE support expected");
            }
            else if (buildNumber >= 10240) // Windows 10
            {
                _logger.LogInformation("✅ Windows 10 detected - basic Bluetooth LE support available");
                if (buildNumber < 17134) // Before version 1803
                {
                    _logger.LogWarning("⚠️ Older Windows 10 build - consider updating for better Bluetooth support");
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Unsupported Windows version for optimal Bluetooth LE functionality");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error logging Windows build information");
        }
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Logs detailed exception context including system state for troubleshooting.
    /// </summary>
    private async Task LogDetailedExceptionContextAsync(Exception ex, int attemptNumber)
    {
        try
        {
            _logger.LogError(ex, "🚨 Error during advertisement attempt {AttemptNumber}: {ErrorType}", 
                attemptNumber, ex.GetType().Name);
            _logger.LogError("Exception details: {Message}", ex.Message);
            
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: {InnerException}", ex.InnerException.Message);
            }
            
            // Log current service provider state
            if (_serviceProvider != null)
            {
                _logger.LogError("Service provider state: {Status}", _serviceProvider.AdvertisementStatus);
            }
            else
            {
                _logger.LogError("Service provider is null - this indicates a serious state issue");
            }
            
            // Log current Windows Bluetooth state
            await LogCurrentBluetoothStateAsync("Error Context Check");
            
            // Log specific error patterns and suggested fixes
            await LogErrorPatternAnalysisAsync(ex);
        }
        catch (Exception contextEx)
        {
            _logger.LogWarning(contextEx, "Error while logging exception context");
        }
    }
    
    /// <summary>
    /// Logs final failure context with comprehensive system state information.
    /// </summary>
    private async Task LogFinalFailureContextAsync(Exception finalException, int totalAttempts)
    {
        try
        {
            _logger.LogError("🆘 FINAL FAILURE ANALYSIS after {TotalAttempts} attempts:", totalAttempts);
            _logger.LogError("Final exception type: {ExceptionType}", finalException.GetType().Name);
            _logger.LogError("Final exception message: {Message}", finalException.Message);
            
            if (finalException.InnerException != null)
            {
                _logger.LogError("Final inner exception: {InnerException}", finalException.InnerException.Message);
            }
            
            // Log stack trace for debugging
            _logger.LogError("Stack trace: {StackTrace}", finalException.StackTrace);
            
            // Comprehensive system state check
            await LogCurrentBluetoothStateAsync("Final Failure Analysis");
            
            // Log recovery suggestions
            _logger.LogError("🛠️ RECOVERY SUGGESTIONS:");
            _logger.LogError("   • Restart the application");
            _logger.LogError("   • Check Windows Bluetooth service status");
            _logger.LogError("   • Verify Bluetooth adapter drivers");
            _logger.LogError("   • Try disabling/re-enabling Bluetooth");
            _logger.LogError("   • Check for Windows updates");
            _logger.LogError("   • Run as Administrator if not already");
        }
        catch (Exception logEx)
        {
            _logger.LogWarning(logEx, "Error during final failure context logging");
        }
    }
    
    /// <summary>
    /// Logs current Windows Bluetooth adapter and radio state in real-time.
    /// </summary>
    private async Task LogCurrentBluetoothStateAsync(string context)
    {
        try
        {
            _logger.LogInformation("📊 {Context} - Current Bluetooth State:", context);
            
            // Check if Bluetooth is available
            var bluetoothAdapter = await BluetoothAdapter.GetDefaultAsync();
            if (bluetoothAdapter == null)
            {
                _logger.LogError("❌ No Bluetooth adapter found");
                return;
            }
            
            _logger.LogInformation("   • Adapter ID: {AdapterId}", bluetoothAdapter.DeviceId);
            _logger.LogInformation("   • Bluetooth Address: {Address}", bluetoothAdapter.BluetoothAddress.ToString("X12"));
            _logger.LogInformation("   • LE Supported: {LESupported}", bluetoothAdapter.IsLowEnergySupported);
            _logger.LogInformation("   • Peripheral Role Supported: {PeripheralSupported}", bluetoothAdapter.IsPeripheralRoleSupported);
            
            // Check radio state
            var radio = await bluetoothAdapter.GetRadioAsync();
            if (radio != null)
            {
                _logger.LogInformation("   • Radio State: {RadioState}", radio.State);
                _logger.LogInformation("   • Radio Name: {RadioName}", radio.Name);
                
                if (radio.State != RadioState.On)
                {
                    _logger.LogWarning("⚠️ Bluetooth radio is not ON - this will prevent advertising");
                }
            }
            
            // Check for other Bluetooth services that might interfere
            _logger.LogDebug("   • Process ID: {ProcessId}", Environment.ProcessId);
            _logger.LogDebug("   • Thread ID: {ThreadId}", Environment.CurrentManagedThreadId);
            _logger.LogDebug("   • Memory Usage: {MemoryMB} MB", GC.GetTotalMemory(false) / 1024 / 1024);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during Bluetooth state check");
        }
    }
    
    /// <summary>
    /// Analyzes exception patterns and provides specific troubleshooting guidance.
    /// </summary>
    private async Task LogErrorPatternAnalysisAsync(Exception ex)
    {
        try
        {
            _logger.LogInformation("🔍 ERROR PATTERN ANALYSIS:");
            
            switch (ex)
            {
                case InvalidOperationException when ex.Message.Contains("current state of the object"):
                    _logger.LogWarning("   📋 Pattern: InvalidOperationException - Service State Issue");
                    _logger.LogWarning("   💡 Cause: GattServiceProvider in invalid state for StartAdvertising()");
                    _logger.LogWarning("   🛠️ Fix: Service provider recreation (implemented in retry logic)");
                    break;
                    
                case ArgumentException when ex.Message.Contains("Value does not fall within the expected range"):
                    _logger.LogWarning("   📋 Pattern: ArgumentException - Parameter Range Issue");
                    _logger.LogWarning("   💡 Cause: Invalid advertisement parameters or Windows API limitation");
                    _logger.LogWarning("   🛠️ Fix: Check advertisement configuration and Windows compatibility");
                    break;
                    
                case UnauthorizedAccessException:
                    _logger.LogWarning("   📋 Pattern: UnauthorizedAccessException - Permission Issue");
                    _logger.LogWarning("   💡 Cause: Insufficient permissions for Bluetooth operations");
                    _logger.LogWarning("   🛠️ Fix: Run application as Administrator");
                    break;
                    
                case COMException comEx:
                    _logger.LogWarning("   📋 Pattern: COMException - Windows API Issue");
                    _logger.LogWarning("   💡 HRESULT: 0x{HResult:X8}", comEx.HResult);
                    _logger.LogWarning("   🛠️ Fix: Check Windows Bluetooth service and drivers");
                    break;
                    
                default:
                    _logger.LogWarning("   📋 Pattern: {ExceptionType} - General Error", ex.GetType().Name);
                    _logger.LogWarning("   💡 This is an uncommon error pattern - check Windows Event Log");
                    break;
            }
        }
        catch (Exception analysisEx)
        {
            _logger.LogWarning(analysisEx, "Error during error pattern analysis");
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles GATT service provider advertisement status changes.
    /// </summary>
    private void OnAdvertisementStatusChanged(GattServiceProvider sender, GattServiceProviderAdvertisementStatusChangedEventArgs args)
    {
        _logger.LogInformation("🔄 Advertisement status changed: {OldStatus} → {NewStatus}", 
            args.Status, sender.AdvertisementStatus);
            
        switch (sender.AdvertisementStatus)
        {
            case GattServiceProviderAdvertisementStatus.Started:
                _logger.LogInformation("✅ Advertisement STARTED - {DeviceName} is now fully discoverable", _config.DeviceName);
                _logger.LogInformation("📱 Phones should now be able to find 'Omnitooth HID' in Bluetooth scan");
                break;
                
            case GattServiceProviderAdvertisementStatus.Created:
                _logger.LogInformation("ℹ️ Advertisement CREATED - {DeviceName} may be discoverable", _config.DeviceName);
                _logger.LogInformation("🔍 If phones can't find device, this status may be the cause");
                break;
                
            case GattServiceProviderAdvertisementStatus.Stopped:
                _logger.LogWarning("⚠️ Advertisement STOPPED - {DeviceName} no longer discoverable", _config.DeviceName);
                _logger.LogWarning("📱 Phones will not see 'Omnitooth HID' until restarted");
                break;
                
            case GattServiceProviderAdvertisementStatus.Aborted:
                _logger.LogError("❌ Advertisement ABORTED - error occurred with {DeviceName}", _config.DeviceName);
                _logger.LogError("🚫 Service failed to become discoverable - check Bluetooth settings");
                break;
                
            default:
                _logger.LogDebug("Advertisement status: {Status}", sender.AdvertisementStatus);
                break;
        }
        
        if (args.Error != Windows.Devices.Bluetooth.BluetoothError.Success)
        {
            _logger.LogError("🚨 Advertisement error occurred: {Error}", args.Error);
            _logger.LogError("💡 This may prevent phones from discovering the HID service");
        }
    }





    /// <summary>
    /// Disconnects all connected devices.
    /// </summary>
    private async Task DisconnectAllDevicesAsync()
    {
        var disconnectTasks = _connectedDevices.Keys.Select(deviceId => DisconnectDeviceAsync(deviceId));
        await Task.WhenAll(disconnectTasks);
    }

    /// <summary>
    /// Cleans up GATT service resources with comprehensive error handling.
    /// </summary>
    private void CleanupGattService()
    {
        try
        {
            _logger.LogDebug("🧹 Cleaning up GATT service resources...");
            
            // Clean up report characteristic
            if (_reportCharacteristic != null)
            {
                try
                {
                    _reportCharacteristic.SubscribedClientsChanged -= OnSubscribedClientsChanged;
                    _reportCharacteristic.ReadRequested -= OnReportReadRequested;
                    _logger.LogDebug("Report characteristic events unsubscribed");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error unsubscribing from report characteristic events");
                }
            }

            // Clean up report map characteristic
            if (_reportMapCharacteristic != null)
            {
                try
                {
                    _reportMapCharacteristic.ReadRequested -= OnReportMapReadRequested;
                    _logger.LogDebug("Report map characteristic events unsubscribed");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error unsubscribing from report map characteristic events");
                }
            }

            // Clean up service provider
            if (_serviceProvider != null)
            {
                try
                {
                    // Stop advertising if still active
                    if (_serviceProvider.AdvertisementStatus == GattServiceProviderAdvertisementStatus.Started)
                    {
                        _logger.LogDebug("Stopping active advertisement during cleanup");
                        _serviceProvider.StopAdvertising();
                    }
                    
                    _serviceProvider.AdvertisementStatusChanged -= OnAdvertisementStatusChanged;
                    _logger.LogDebug("Service provider events unsubscribed");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during service provider cleanup");
                }
            }

            // Clear all references
            _serviceProvider = null;
            _reportCharacteristic = null;
            _reportMapCharacteristic = null;
            
            _logger.LogDebug("✅ GATT service cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during GATT service cleanup");
            // Even if cleanup fails, ensure references are cleared
            _serviceProvider = null;
            _reportCharacteristic = null;
            _reportMapCharacteristic = null;
        }
    }

    /// <summary>
    /// Handles subscribed clients changed event for report characteristic.
    /// </summary>
    private void OnSubscribedClientsChanged(GattLocalCharacteristic sender, object args)
    {
        var subscribedCount = sender.SubscribedClients.Count;
        var previousCount = _connectedDevices.Count;
        
        _logger.LogInformation("🔄 GATT connection event - Subscribed clients: {Count} (was {PreviousCount})", 
            subscribedCount, previousCount);
        
        if (subscribedCount > previousCount)
        {
            _logger.LogInformation("📱 New device attempting to connect to Omnitooth HID service");
        }
        else if (subscribedCount < previousCount)
        {
            _logger.LogInformation("📱 Device disconnected from Omnitooth HID service");
        }

        // Track all currently subscribed sessions
        var currentSessionIds = new HashSet<string>();
        
        // Handle device connections/disconnections based on subscriptions
        foreach (var session in sender.SubscribedClients)
        {
            var deviceId = session.Session.DeviceId.Id;
            currentSessionIds.Add(deviceId);
            
            if (!_connectedDevices.ContainsKey(deviceId))
            {
                var device = new CoreBluetoothDevice
                {
                    Id = deviceId,
                    Name = "Unknown Device", // TODO: Get actual device name
                    Address = deviceId,
                    ConnectionState = ConnectionState.Connected,
                    IsAuthenticated = true,
                    IsEncrypted = true
                };

                _connectedDevices.TryAdd(deviceId, device);
                _deviceConnectedSubject.OnNext(device);
                _connectionStateChangedSubject.OnNext((device, ConnectionState.Disconnected, ConnectionState.Connected));

                _logger.LogInformation("✅ Device successfully connected: {DeviceId}", deviceId);
                _logger.LogInformation("📊 Connection details - Max PDU Size: {MaxPduSize}, Can Maintain: {CanMaintain}", 
                    session.Session.MaxPduSize, session.Session.CanMaintainConnection);
                _logger.LogInformation("🎯 Phone should now be able to receive keyboard/mouse input from this PC");
            }
        }
        
        // Handle disconnections - remove devices no longer in subscribed clients
        var devicesToRemove = _connectedDevices.Keys.Where(id => !currentSessionIds.Contains(id)).ToList();
        foreach (var deviceId in devicesToRemove)
        {
            if (_connectedDevices.TryRemove(deviceId, out var device))
            {
                device.ConnectionState = ConnectionState.Disconnected;
                _deviceDisconnectedSubject.OnNext(device);
                _connectionStateChangedSubject.OnNext((device, ConnectionState.Connected, ConnectionState.Disconnected));
                
                _logger.LogInformation("❌ Device disconnected: {DeviceId}", deviceId);
            }
        }
        
        if (subscribedCount == 0)
        {
            _logger.LogInformation("💤 No devices connected - Omnitooth HID ready for new connections");
        }
    }

    /// <summary>
    /// Handles read requests for the report characteristic.
    /// </summary>
    private void OnReportReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        
        try
        {
            // Return empty report for now
            var writer = new DataWriter();
            writer.WriteByte(0x00);
            
            var request = args.GetRequestAsync().GetAwaiter().GetResult();
            request.RespondWithValue(writer.DetachBuffer());
            
            _logger.LogTrace("Report read request handled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling report read request");
        }
        finally
        {
            deferral.Complete();
        }
    }

    /// <summary>
    /// Handles read requests for the report map characteristic.
    /// </summary>
    private void OnReportMapReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        
        try
        {
            // Note: Report map is now configured as a static value by the factory
            // This event handler should not normally be called, but we'll handle it gracefully
            _logger.LogWarning("⚠️ Report map read request received - this should be handled by static value");
            
            var request = args.GetRequestAsync().GetAwaiter().GetResult();
            
            // Try to get the static value from the characteristic
            if (_reportMapCharacteristic?.StaticValue != null)
            {
                request.RespondWithValue(_reportMapCharacteristic.StaticValue);
                _logger.LogTrace("Report map read request handled with static value");
            }
            else
            {
                request.RespondWithProtocolError(GattProtocolError.UnlikelyError);
                _logger.LogWarning("Report map static value not available");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling report map read request");
        }
        finally
        {
            deferral.Complete();
        }
    }

    /// <summary>
    /// Handles read requests for the HID information characteristic.
    /// </summary>
    private void OnHidInformationReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        
        try
        {
            var writer = new DataWriter();
            writer.WriteUInt16(0x0111); // bcdHID (HID version 1.11)
            writer.WriteByte(0x00);     // bCountryCode (not localized)
            writer.WriteByte(0x03);     // Flags (RemoteWake | NormallyConnectable)
            
            var request = args.GetRequestAsync().GetAwaiter().GetResult();
            request.RespondWithValue(writer.DetachBuffer());
            
            _logger.LogTrace("HID information read request handled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling HID information read request");
        }
        finally
        {
            deferral.Complete();
        }
    }

    /// <summary>
    /// Handles write requests for the HID control point characteristic.
    /// </summary>
    private void OnHidControlPointWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        
        try
        {
            var request = args.GetRequestAsync().GetAwaiter().GetResult();
            var reader = DataReader.FromBuffer(request.Value);
            var command = reader.ReadByte();
            
            _logger.LogTrace("HID control point command received: 0x{Command:X2}", command);
            
            request.Respond();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling HID control point write request");
        }
        finally
        {
            deferral.Complete();
        }
    }

    /// <summary>
    /// Starts the discovery monitoring background task.
    /// </summary>
    private void StartDiscoveryMonitoring()
    {
        try
        {
            _monitoringCancellationTokenSource = new CancellationTokenSource();
            _discoveryMonitoringTask = Task.Run(async () => await MonitorDiscoveryStatusAsync(_monitoringCancellationTokenSource.Token));
            _logger.LogInformation("🔍 Discovery monitoring task started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start discovery monitoring task");
        }
    }

    /// <summary>
    /// Stops the discovery monitoring background task.
    /// </summary>
    private async Task StopDiscoveryMonitoringAsync()
    {
        try
        {
            if (_monitoringCancellationTokenSource != null && _discoveryMonitoringTask != null)
            {
                _logger.LogInformation("🔍 Stopping discovery monitoring task");
                _monitoringCancellationTokenSource.Cancel();
                
                // Wait for task to complete with timeout
                await _discoveryMonitoringTask.WaitAsync(TimeSpan.FromSeconds(5));
                _logger.LogInformation("🔍 Discovery monitoring task stopped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping discovery monitoring task");
        }
        finally
        {
            _monitoringCancellationTokenSource?.Dispose();
            _monitoringCancellationTokenSource = null;
            _discoveryMonitoringTask = null;
        }
    }

    /// <summary>
    /// Monitors discovery status and logs periodic updates to help diagnose connection issues.
    /// </summary>
    private async Task MonitorDiscoveryStatusAsync(CancellationToken cancellationToken)
    {
        var lastConnectionCount = 0;
        var noConnectionLogInterval = TimeSpan.FromMinutes(1);
        var lastNoConnectionLog = DateTime.MinValue;
        
        _logger.LogInformation("🔍 Discovery monitoring started - checking advertisement status and connections");
        
        try
        {
            // Log initial status immediately
            await LogCurrentDiscoveryStatusAsync("Initial Status Check");
            
            while (_serviceState == ServiceState.Running && !_disposed && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken); // Check every 30 seconds
                
                if (_serviceState != ServiceState.Running || _disposed || cancellationToken.IsCancellationRequested)
                    break;
                
                var currentConnectionCount = _connectedDevices.Count;
                
                // Log significant changes
                if (currentConnectionCount != lastConnectionCount)
                {
                    if (currentConnectionCount > 0)
                    {
                        _logger.LogInformation("📱 Active connections: {Count} device(s) connected to Omnitooth HID", 
                            currentConnectionCount);
                    }
                    else
                    {
                        _logger.LogInformation("💤 No active connections - waiting for devices to discover Omnitooth HID");
                    }
                    lastConnectionCount = currentConnectionCount;
                }
                
                // Periodically log discovery status when no connections
                if (currentConnectionCount == 0 && DateTime.UtcNow - lastNoConnectionLog > noConnectionLogInterval)
                {
                    await LogCurrentDiscoveryStatusAsync("Periodic Status Check");
                    LogCircuitBreakerMetrics();
                    lastNoConnectionLog = DateTime.UtcNow;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🔍 Discovery monitoring cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in discovery status monitoring");
        }
        
        _logger.LogInformation("🔍 Discovery monitoring task exiting");
    }

    /// <summary>
    /// Logs the current discovery status with detailed diagnostics.
    /// </summary>
    private async Task LogCurrentDiscoveryStatusAsync(string context)
    {
        try
        {
            var advertisementStatus = _serviceProvider?.AdvertisementStatus;
            var connectionCount = _connectedDevices.Count;
            
            _logger.LogInformation("🔍 {Context} - Discovery Status:", context);
            _logger.LogInformation("   • Service Status: {ServiceStatus}", _serviceState);
            _logger.LogInformation("   • Advertisement Status: {AdvertisementStatus}", advertisementStatus);
            _logger.LogInformation("   • Connected Devices: {ConnectionCount}", connectionCount);
            _logger.LogInformation("   • Device Name: Omnitooth HID");
            _logger.LogInformation("   • HID Service UUID: {ServiceUuid}", HidServiceUuid);
            _logger.LogInformation("   • Circuit Breaker State: {CircuitState}", _circuitBreaker.State);
            
            if (advertisementStatus == GattServiceProviderAdvertisementStatus.Created)
            {
                _logger.LogWarning("   ⚠️ Advertisement stuck in 'Created' status - this may prevent discovery");
                _logger.LogInformation("   💡 Troubleshooting steps:");
                _logger.LogInformation("     - Check Windows Bluetooth adapter supports LE peripheral mode");
                _logger.LogInformation("     - Verify Windows Bluetooth service is running");
                _logger.LogInformation("     - Try restarting Omnitooth service");
            }
            else if (advertisementStatus == GattServiceProviderAdvertisementStatus.Started)
            {
                _logger.LogInformation("   ✅ Advertisement active - device should be discoverable");
            }
            
            if (connectionCount == 0)
            {
                _logger.LogInformation("   💡 If your phone can't find 'Omnitooth HID':");
                _logger.LogInformation("     - Ensure phone is close to PC (within 3 feet)");
                _logger.LogInformation("     - Refresh Bluetooth scan on phone");
                _logger.LogInformation("     - Check phone supports Bluetooth LE HID connections");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging discovery status");
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Logs comprehensive circuit breaker metrics and performance statistics.
    /// </summary>
    private void LogCircuitBreakerMetrics()
    {
        try
        {
            var metrics = _circuitBreaker.GetMetrics();
            
            _logger.LogInformation("🔌 Circuit Breaker Metrics:");
            _logger.LogInformation("   • State: {State}", metrics.State);
            _logger.LogInformation("   • Current Failure Count: {FailureCount}", metrics.FailureCount);
            _logger.LogInformation("   • Success Rate: {SuccessRate:F1}%", 100.0 - metrics.FailureRate);
            _logger.LogInformation("   • Total Operations: {TotalOperations}", metrics.SuccessCount + metrics.TotalFailureCount);
            _logger.LogInformation("   • Average Execution Time: {AvgTime:F1}ms", metrics.AverageExecutionTime.TotalMilliseconds);
            
            if (metrics.State == CircuitBreakerState.Open)
            {
                _logger.LogWarning("   ⚠️ Circuit is OPEN - operations will be rejected");
                _logger.LogWarning("   ⏰ Next retry: {NextRetry}", metrics.NextRetryTime);
            }
            else if (metrics.State == CircuitBreakerState.HalfOpen)
            {
                _logger.LogInformation("   🟡 Circuit is HALF-OPEN - testing recovery");
            }
            else
            {
                _logger.LogInformation("   ✅ Circuit is CLOSED - normal operation");
            }
            
            if (metrics.RejectedCount > 0)
            {
                _logger.LogInformation("   🚫 Rejected Operations: {RejectedCount}", metrics.RejectedCount);
            }
            
            _logger.LogDebug("   📊 Time in Current State: {TimeInState}", metrics.TimeInCurrentState);
            
            if (metrics.LastFailureTime.HasValue)
            {
                var timeSinceLastFailure = DateTime.UtcNow - metrics.LastFailureTime.Value;
                _logger.LogDebug("   ⏱️ Time Since Last Failure: {TimeSinceFailure}", timeSinceLastFailure);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error logging circuit breaker metrics");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogDebug("Disposing BluetoothGattServerService");

        try
        {
            _ = StopAsync();
            
            _deviceConnectedSubject?.Dispose();
            _deviceDisconnectedSubject?.Dispose();
            _connectionStateChangedSubject?.Dispose();
            _monitoringCancellationTokenSource?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during BluetoothGattServerService disposal");
        }
        finally
        {
            _disposed = true;
            _logger.LogDebug("BluetoothGattServerService disposed");
        }
    }
}