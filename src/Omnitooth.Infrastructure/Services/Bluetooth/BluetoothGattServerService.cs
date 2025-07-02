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
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace Omnitooth.Infrastructure.Services.Bluetooth;

/// <summary>
/// Bluetooth GATT server service implementing HID over GATT Profile (HOGP).
/// </summary>
public sealed class BluetoothGattServerService : IBluetoothService
{
    private readonly ILogger<BluetoothGattServerService> _logger;
    private readonly BluetoothConfiguration _config;
    private readonly Subject<CoreBluetoothDevice> _deviceConnectedSubject = new();
    private readonly Subject<CoreBluetoothDevice> _deviceDisconnectedSubject = new();
    private readonly Subject<(CoreBluetoothDevice Device, ConnectionState OldState, ConnectionState NewState)> _connectionStateChangedSubject = new();
    private readonly ConcurrentDictionary<string, CoreBluetoothDevice> _connectedDevices = new();
    
    private GattServiceProvider? _serviceProvider;
    private BluetoothLEAdvertisementPublisher? _advertisementPublisher;
    private GattLocalCharacteristic? _reportCharacteristic;
    private GattLocalCharacteristic? _reportMapCharacteristic;
    private ServiceState _serviceState = ServiceState.Stopped;
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
    public BluetoothGattServerService(ILogger<BluetoothGattServerService> logger, IOptions<BluetoothConfiguration> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = options?.Value ?? throw new ArgumentNullException(nameof(options));
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
    public bool IsAdvertising => _advertisementPublisher?.Status == BluetoothLEAdvertisementPublisherStatus.Started;

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
            // Create GATT service provider
            await CreateGattServiceAsync();

            // Start Bluetooth LE advertisement for device discovery
            await StartBluetoothAdvertisementAsync();

            _serviceState = ServiceState.Running;
            _logger.LogInformation("Bluetooth GATT server started successfully");
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
        _serviceState = ServiceState.Stopping;

        try
        {
            // Stop BLE advertising
            StopBluetoothAdvertisement();

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
                await _reportCharacteristic.NotifyValueAsync(writer.DetachBuffer());
                device.UpdateActivity();
                
                _logger.LogTrace("Sent HID report to device {DeviceId}: {ReportType} ({DataLength} bytes)",
                    deviceId, report.ReportType, report.Data.Length);
            }
            else
            {
                _logger.LogDebug("No subscribed clients for device {DeviceId}", deviceId);
            }
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
    /// Creates the GATT service with HID characteristics.
    /// </summary>
    private async Task CreateGattServiceAsync()
    {
        _logger.LogDebug("Creating HID GATT service");

        var serviceResult = await GattServiceProvider.CreateAsync(HidServiceUuid);
        if (serviceResult.Error != Windows.Devices.Bluetooth.BluetoothError.Success)
        {
            throw new InvalidOperationException($"Failed to create GATT service: {serviceResult.Error}");
        }

        _serviceProvider = serviceResult.ServiceProvider;

        // Create report characteristic (input reports)
        await CreateReportCharacteristicAsync();

        // Create report map characteristic (HID descriptor)
        await CreateReportMapCharacteristicAsync();

        // Create HID information characteristic
        await CreateHidInformationCharacteristicAsync();

        // Create HID control point characteristic
        await CreateHidControlPointCharacteristicAsync();

        // Service will be started when we call StartAdvertising later
    }

    /// <summary>
    /// Starts Bluetooth LE advertisement for device discovery.
    /// </summary>
    private async Task StartBluetoothAdvertisementAsync()
    {
        _logger.LogDebug("Starting Bluetooth LE advertisement for device discovery");

        _advertisementPublisher = new BluetoothLEAdvertisementPublisher();

        // Configure advertisement for HID device discovery
        var advertisement = _advertisementPublisher.Advertisement;
        
        // Set device name for discovery
        advertisement.LocalName = _config.DeviceName;
        
        // Add HID service UUID to advertisement
        advertisement.ServiceUuids.Add(HidServiceUuid);
        
        // Set HID keyboard appearance (0x03C1 = 961 decimal)
        // This tells phones this is a keyboard device
        var appearanceData = new byte[] { 0xC1, 0x03 }; // Little-endian format
        advertisement.ManufacturerData.Add(new BluetoothLEManufacturerData
        {
            CompanyId = 0xFFFF, // Generic company ID
            Data = appearanceData.AsBuffer()
        });
        
        // Set advertisement flags for discovery and connection
        advertisement.Flags = BluetoothLEAdvertisementFlags.GeneralDiscoverableMode |
                             BluetoothLEAdvertisementFlags.ClassicNotSupported;

        // Configure advertising parameters
        _advertisementPublisher.UseExtendedAdvertisement = false;
        _advertisementPublisher.PreferredTransmitPowerLevelInDBm = 0;

        // Subscribe to status changes
        _advertisementPublisher.StatusChanged += OnAdvertisementStatusChanged;

        // Start advertising
        _advertisementPublisher.Start();
        
        // Give time for advertising to start
        await Task.Delay(100);
        
        _logger.LogInformation("Bluetooth LE advertisement started - Device: {DeviceName}", _config.DeviceName);
    }

    /// <summary>
    /// Handles advertisement status changes.
    /// </summary>
    private void OnAdvertisementStatusChanged(BluetoothLEAdvertisementPublisher sender, BluetoothLEAdvertisementPublisherStatusChangedEventArgs args)
    {
        _logger.LogInformation("Advertisement status changed: {Status}", args.Status);
        
        if (args.Error != Windows.Devices.Bluetooth.BluetoothError.Success)
        {
            _logger.LogError("Advertisement error: {Error}", args.Error);
        }
        else if (args.Status == BluetoothLEAdvertisementPublisherStatus.Started)
        {
            _logger.LogInformation("Device is now discoverable as: {DeviceName}", _config.DeviceName);
        }
    }

    /// <summary>
    /// Stops Bluetooth LE advertisement.
    /// </summary>
    private void StopBluetoothAdvertisement()
    {
        if (_advertisementPublisher != null)
        {
            _advertisementPublisher.StatusChanged -= OnAdvertisementStatusChanged;
            
            if (_advertisementPublisher.Status == BluetoothLEAdvertisementPublisherStatus.Started)
            {
                _advertisementPublisher.Stop();
                _logger.LogInformation("Bluetooth LE advertisement stopped");
            }
            
            _advertisementPublisher = null;
        }
    }

    /// <summary>
    /// Creates the report characteristic for sending input reports.
    /// </summary>
    private async Task CreateReportCharacteristicAsync()
    {
        var parameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read | 
                                     GattCharacteristicProperties.Notify,
            UserDescription = "HID Report",
            ReadProtectionLevel = GattProtectionLevel.EncryptionRequired,
            WriteProtectionLevel = GattProtectionLevel.EncryptionRequired
        };

        var result = await _serviceProvider!.Service.CreateCharacteristicAsync(
            ReportCharacteristicUuid, parameters);

        if (result.Error != Windows.Devices.Bluetooth.BluetoothError.Success)
        {
            throw new InvalidOperationException($"Failed to create report characteristic: {result.Error}");
        }

        _reportCharacteristic = result.Characteristic;
        _reportCharacteristic.SubscribedClientsChanged += OnSubscribedClientsChanged;
        _reportCharacteristic.ReadRequested += OnReportReadRequested;

        _logger.LogDebug("Created report characteristic");
    }

    /// <summary>
    /// Creates the report map characteristic containing the HID descriptor.
    /// </summary>
    private async Task CreateReportMapCharacteristicAsync()
    {
        var parameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read,
            UserDescription = "HID Report Map",
            ReadProtectionLevel = GattProtectionLevel.EncryptionRequired
        };

        var result = await _serviceProvider!.Service.CreateCharacteristicAsync(
            ReportMapCharacteristicUuid, parameters);

        if (result.Error != Windows.Devices.Bluetooth.BluetoothError.Success)
        {
            throw new InvalidOperationException($"Failed to create report map characteristic: {result.Error}");
        }

        _reportMapCharacteristic = result.Characteristic;
        _reportMapCharacteristic.ReadRequested += OnReportMapReadRequested;

        _logger.LogDebug("Created report map characteristic");
    }

    /// <summary>
    /// Creates the HID information characteristic.
    /// </summary>
    private async Task CreateHidInformationCharacteristicAsync()
    {
        var parameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read,
            UserDescription = "HID Information",
            ReadProtectionLevel = GattProtectionLevel.EncryptionRequired
        };

        var result = await _serviceProvider!.Service.CreateCharacteristicAsync(
            HidInformationCharacteristicUuid, parameters);

        if (result.Error != Windows.Devices.Bluetooth.BluetoothError.Success)
        {
            throw new InvalidOperationException($"Failed to create HID information characteristic: {result.Error}");
        }

        result.Characteristic.ReadRequested += OnHidInformationReadRequested;
        _logger.LogDebug("Created HID information characteristic");
    }

    /// <summary>
    /// Creates the HID control point characteristic.
    /// </summary>
    private async Task CreateHidControlPointCharacteristicAsync()
    {
        var parameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.WriteWithoutResponse,
            UserDescription = "HID Control Point",
            WriteProtectionLevel = GattProtectionLevel.EncryptionRequired
        };

        var result = await _serviceProvider!.Service.CreateCharacteristicAsync(
            HidControlPointCharacteristicUuid, parameters);

        if (result.Error != Windows.Devices.Bluetooth.BluetoothError.Success)
        {
            throw new InvalidOperationException($"Failed to create HID control point characteristic: {result.Error}");
        }

        result.Characteristic.WriteRequested += OnHidControlPointWriteRequested;
        _logger.LogDebug("Created HID control point characteristic");
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
    /// Cleans up GATT service resources.
    /// </summary>
    private void CleanupGattService()
    {
        if (_reportCharacteristic != null)
        {
            _reportCharacteristic.SubscribedClientsChanged -= OnSubscribedClientsChanged;
            _reportCharacteristic.ReadRequested -= OnReportReadRequested;
        }

        if (_reportMapCharacteristic != null)
        {
            _reportMapCharacteristic.ReadRequested -= OnReportMapReadRequested;
        }

        _serviceProvider = null;
        _reportCharacteristic = null;
        _reportMapCharacteristic = null;
    }

    /// <summary>
    /// Handles subscribed clients changed event for report characteristic.
    /// </summary>
    private void OnSubscribedClientsChanged(GattLocalCharacteristic sender, object args)
    {
        var subscribedCount = sender.SubscribedClients.Count;
        _logger.LogInformation("GATT connection event - Subscribed clients: {Count}", subscribedCount);
        
        if (subscribedCount > 0)
        {
            _logger.LogInformation("Device attempting to connect to HID service");
        }

        // Handle device connections/disconnections based on subscriptions
        foreach (var session in sender.SubscribedClients)
        {
            var deviceId = session.Session.DeviceId.Id;
            
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

                _logger.LogInformation("Device connected: {DeviceId}", deviceId);
            }
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
            // TODO: Return actual HID report descriptor
            var writer = new DataWriter();
            var reportDescriptor = GetCombinedReportDescriptor();
            writer.WriteBytes(reportDescriptor);
            
            var request = args.GetRequestAsync().GetAwaiter().GetResult();
            request.RespondWithValue(writer.DetachBuffer());
            
            _logger.LogTrace("Report map read request handled");
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
    /// Gets the combined HID report descriptor for keyboard and mouse.
    /// </summary>
    /// <returns>The HID report descriptor bytes.</returns>
    private static byte[] GetCombinedReportDescriptor()
    {
        // Simplified combined keyboard + mouse HID report descriptor
        return new byte[]
        {
            // Keyboard
            0x05, 0x01,        // Usage Page (Generic Desktop)
            0x09, 0x06,        // Usage (Keyboard)
            0xA1, 0x01,        // Collection (Application)
            0x85, 0x01,        //   Report ID (1)
            0x05, 0x07,        //   Usage Page (Keyboard/Keypad)
            0x19, 0xE0,        //   Usage Minimum (224)
            0x29, 0xE7,        //   Usage Maximum (231)
            0x15, 0x00,        //   Logical Minimum (0)
            0x25, 0x01,        //   Logical Maximum (1)
            0x75, 0x01,        //   Report Size (1)
            0x95, 0x08,        //   Report Count (8)
            0x81, 0x02,        //   Input (Data, Variable, Absolute)
            0x95, 0x01,        //   Report Count (1)
            0x75, 0x08,        //   Report Size (8)
            0x81, 0x01,        //   Input (Constant)
            0x95, 0x06,        //   Report Count (6)
            0x75, 0x08,        //   Report Size (8)
            0x15, 0x00,        //   Logical Minimum (0)
            0x25, 0x65,        //   Logical Maximum (101)
            0x05, 0x07,        //   Usage Page (Keyboard/Keypad)
            0x19, 0x00,        //   Usage Minimum (0)
            0x29, 0x65,        //   Usage Maximum (101)
            0x81, 0x00,        //   Input (Data, Array)
            0xC0,              // End Collection
            
            // Mouse
            0x05, 0x01,        // Usage Page (Generic Desktop)
            0x09, 0x02,        // Usage (Mouse)
            0xA1, 0x01,        // Collection (Application)
            0x85, 0x02,        //   Report ID (2)
            0x09, 0x01,        //   Usage (Pointer)
            0xA1, 0x00,        //   Collection (Physical)
            0x05, 0x09,        //     Usage Page (Button)
            0x19, 0x01,        //     Usage Minimum (1)
            0x29, 0x03,        //     Usage Maximum (3)
            0x15, 0x00,        //     Logical Minimum (0)
            0x25, 0x01,        //     Logical Maximum (1)
            0x95, 0x03,        //     Report Count (3)
            0x75, 0x01,        //     Report Size (1)
            0x81, 0x02,        //     Input (Data, Variable, Absolute)
            0x95, 0x01,        //     Report Count (1)
            0x75, 0x05,        //     Report Size (5)
            0x81, 0x01,        //     Input (Constant)
            0x05, 0x01,        //     Usage Page (Generic Desktop)
            0x09, 0x30,        //     Usage (X)
            0x09, 0x31,        //     Usage (Y)
            0x09, 0x38,        //     Usage (Wheel)
            0x15, 0x81,        //     Logical Minimum (-127)
            0x25, 0x7F,        //     Logical Maximum (127)
            0x75, 0x08,        //     Report Size (8)
            0x95, 0x03,        //     Report Count (3)
            0x81, 0x06,        //     Input (Data, Variable, Relative)
            0xC0,              //   End Collection
            0xC0               // End Collection
        };
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