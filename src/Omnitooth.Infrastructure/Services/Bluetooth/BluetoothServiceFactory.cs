using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Omnitooth.Core.Configuration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using Windows.Storage.Streams;

namespace Omnitooth.Infrastructure.Services.Bluetooth;

/// <summary>
/// Factory for creating and validating Bluetooth GATT service providers.
/// Provides clean service creation with built-in health checks and validation.
/// </summary>
public class BluetoothServiceFactory : IBluetoothServiceFactory
{
    private readonly ILogger<BluetoothServiceFactory> _logger;
    private readonly BluetoothConfiguration _config;
    
    // HID over GATT Profile UUIDs
    private static readonly Guid HidServiceUuid = new("00001812-0000-1000-8000-00805f9b34fb");
    private static readonly Guid ReportCharacteristicUuid = new("00002a4d-0000-1000-8000-00805f9b34fb");
    private static readonly Guid ReportMapCharacteristicUuid = new("00002a4b-0000-1000-8000-00805f9b34fb");
    private static readonly Guid HidInformationCharacteristicUuid = new("00002a4a-0000-1000-8000-00805f9b34fb");
    private static readonly Guid HidControlPointCharacteristicUuid = new("00002a4c-0000-1000-8000-00805f9b34fb");

    /// <summary>
    /// Initializes a new instance of the <see cref="BluetoothServiceFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">Bluetooth configuration options.</param>
    public BluetoothServiceFactory(ILogger<BluetoothServiceFactory> logger, IOptions<BluetoothConfiguration> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<GattServiceProvider> CreateHidServiceProviderAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🏭 Creating new HID GATT service provider with factory");
            
            // Create the base GATT service provider
            var serviceResult = await GattServiceProvider.CreateAsync(HidServiceUuid);
            if (serviceResult.Error != BluetoothError.Success)
            {
                throw new InvalidOperationException($"Failed to create GATT service provider: {serviceResult.Error}");
            }

            var serviceProvider = serviceResult.ServiceProvider;
            _logger.LogDebug("✅ Base GATT service provider created successfully");

            // Configure all HID characteristics
            await ConfigureHidCharacteristicsAsync(serviceProvider, cancellationToken);
            
            // Validate the created service
            if (!ValidateServiceProvider(serviceProvider))
            {
                await DisposeServiceProviderAsync(serviceProvider, cancellationToken);
                throw new InvalidOperationException("Service provider validation failed after creation");
            }
            
            // Perform comprehensive health check
            var healthResult = await PerformHealthCheckAsync(serviceProvider, cancellationToken);
            if (!healthResult.IsHealthy)
            {
                await DisposeServiceProviderAsync(serviceProvider, cancellationToken);
                throw new InvalidOperationException($"Service provider health check failed: {healthResult.StatusMessage}");
            }
            
            _logger.LogInformation("✅ HID GATT service provider created and validated successfully");
            return serviceProvider;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to create HID GATT service provider");
            throw;
        }
    }

    /// <inheritdoc />
    public bool ValidateServiceProvider(GattServiceProvider serviceProvider)
    {
        try
        {
            if (serviceProvider == null)
            {
                _logger.LogWarning("❌ Service provider is null");
                return false;
            }

            // Check advertisement status is in a valid initial state
            var status = serviceProvider.AdvertisementStatus;
            if (status != GattServiceProviderAdvertisementStatus.Created && 
                status != GattServiceProviderAdvertisementStatus.Stopped)
            {
                _logger.LogWarning("❌ Service provider in invalid initial state: {Status}", status);
                return false;
            }

            // Validate that the service has the expected characteristics
            if (serviceProvider.Service?.Characteristics?.Count == 0)
            {
                _logger.LogWarning("❌ Service provider has no characteristics configured");
                return false;
            }

            _logger.LogDebug("✅ Service provider validation passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during service provider validation");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceHealthResult> PerformHealthCheckAsync(GattServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        try
        {
            var issues = new List<string>();
            
            // Check service provider state
            if (serviceProvider == null)
            {
                return ServiceHealthResult.Unhealthy("Service provider is null", "Service provider instance is null");
            }

            // Check advertisement status
            var status = serviceProvider.AdvertisementStatus;
            if (status == GattServiceProviderAdvertisementStatus.Aborted)
            {
                issues.Add("Advertisement status is 'Aborted'");
            }

            // Check service configuration
            var service = serviceProvider.Service;
            if (service == null)
            {
                issues.Add("GATT service is null");
            }
            else
            {
                // Validate service UUID
                if (service.Uuid != HidServiceUuid)
                {
                    issues.Add($"Service UUID mismatch. Expected: {HidServiceUuid}, Actual: {service.Uuid}");
                }

                // Validate characteristics count
                var characteristicsCount = service.Characteristics?.Count ?? 0;
                if (characteristicsCount == 0)
                {
                    issues.Add("No characteristics configured for the service");
                }
                else
                {
                    _logger.LogDebug("Service has {CharacteristicsCount} characteristics configured", characteristicsCount);
                }
            }

            // Check Windows Bluetooth adapter availability
            var bluetoothAdapter = await BluetoothAdapter.GetDefaultAsync();
            if (bluetoothAdapter == null)
            {
                issues.Add("No Bluetooth adapter available");
            }
            else if (!bluetoothAdapter.IsLowEnergySupported)
            {
                issues.Add("Bluetooth adapter does not support Low Energy");
            }

            // Return health result
            if (issues.Count == 0)
            {
                _logger.LogDebug("✅ Service health check passed");
                return ServiceHealthResult.Healthy("Service provider is healthy and ready for operation");
            }
            else
            {
                _logger.LogWarning("❌ Service health check found {IssueCount} issues", issues.Count);
                return ServiceHealthResult.Unhealthy("Service provider has health issues", issues.ToArray());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during service health check");
            return ServiceHealthResult.Unhealthy($"Health check failed with exception: {ex.Message}", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task DisposeServiceProviderAsync(GattServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        try
        {
            if (serviceProvider == null)
            {
                return;
            }

            _logger.LogDebug("🗑️ Disposing GATT service provider");

            // Stop advertising if active
            if (serviceProvider.AdvertisementStatus == GattServiceProviderAdvertisementStatus.Started)
            {
                serviceProvider.StopAdvertising();
                await Task.Delay(500, cancellationToken); // Brief wait for stop to complete
            }

            // Note: GattServiceProvider doesn't implement IDisposable in Windows Runtime
            // The system will handle cleanup when references are released
            
            _logger.LogDebug("✅ GATT service provider disposal completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error during service provider disposal");
        }
    }

    /// <summary>
    /// Configures all required HID characteristics for the GATT service.
    /// </summary>
    private async Task ConfigureHidCharacteristicsAsync(GattServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("🔧 Configuring HID characteristics...");

            // Create report characteristic (input reports)
            await CreateReportCharacteristicAsync(serviceProvider.Service);

            // Create report map characteristic (HID descriptor)
            await CreateReportMapCharacteristicAsync(serviceProvider.Service);

            // Create HID information characteristic
            await CreateHidInformationCharacteristicAsync(serviceProvider.Service);

            // Create HID control point characteristic
            await CreateHidControlPointCharacteristicAsync(serviceProvider.Service);

            _logger.LogDebug("✅ HID characteristics configured successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to configure HID characteristics");
            throw;
        }
    }

    /// <summary>
    /// Creates the report characteristic for sending input reports.
    /// </summary>
    private async Task CreateReportCharacteristicAsync(GattLocalService service)
    {
        var parameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read | 
                                     GattCharacteristicProperties.Notify,
            UserDescription = "HID Report",
            PresentationFormats = { GattPresentationFormat.FromParts(
                (byte)GattPresentationFormatTypes.Struct,
                1,
                0x2700, // Unitless format unit
                1,
                0) }
        };

        var characteristicResult = await service.CreateCharacteristicAsync(ReportCharacteristicUuid, parameters);
        if (characteristicResult.Error != BluetoothError.Success)
        {
            throw new InvalidOperationException($"Failed to create report characteristic: {characteristicResult.Error}");
        }

        _logger.LogDebug("✅ Report characteristic created");
    }

    /// <summary>
    /// Creates the report map characteristic containing the HID descriptor.
    /// </summary>
    private async Task CreateReportMapCharacteristicAsync(GattLocalService service)
    {
        var reportDescriptor = GetCombinedReportDescriptor();
        var writer = new DataWriter();
        writer.WriteBytes(reportDescriptor);

        var parameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read,
            UserDescription = "HID Report Map",
            StaticValue = writer.DetachBuffer(),
            PresentationFormats = { GattPresentationFormat.FromParts(
                (byte)GattPresentationFormatTypes.Struct,
                1,
                0x2700, // Unitless format unit
                1,
                0) }
        };

        var characteristicResult = await service.CreateCharacteristicAsync(ReportMapCharacteristicUuid, parameters);
        if (characteristicResult.Error != BluetoothError.Success)
        {
            throw new InvalidOperationException($"Failed to create report map characteristic: {characteristicResult.Error}");
        }

        _logger.LogDebug("✅ Report map characteristic created with {DescriptorSize} bytes", reportDescriptor.Length);
    }

    /// <summary>
    /// Creates the HID information characteristic.
    /// </summary>
    private async Task CreateHidInformationCharacteristicAsync(GattLocalService service)
    {
        var hidInfo = new byte[] { 0x11, 0x01, 0x00, 0x03 }; // Version 1.11, Country 0, Flags 3
        var writer = new DataWriter();
        writer.WriteBytes(hidInfo);

        var parameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read,
            UserDescription = "HID Information",
            StaticValue = writer.DetachBuffer()
        };

        var characteristicResult = await service.CreateCharacteristicAsync(HidInformationCharacteristicUuid, parameters);
        if (characteristicResult.Error != BluetoothError.Success)
        {
            throw new InvalidOperationException($"Failed to create HID information characteristic: {characteristicResult.Error}");
        }

        _logger.LogDebug("✅ HID information characteristic created");
    }

    /// <summary>
    /// Creates the HID control point characteristic.
    /// </summary>
    private async Task CreateHidControlPointCharacteristicAsync(GattLocalService service)
    {
        var parameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.WriteWithoutResponse,
            UserDescription = "HID Control Point"
        };

        var characteristicResult = await service.CreateCharacteristicAsync(HidControlPointCharacteristicUuid, parameters);
        if (characteristicResult.Error != BluetoothError.Success)
        {
            throw new InvalidOperationException($"Failed to create HID control point characteristic: {characteristicResult.Error}");
        }

        _logger.LogDebug("✅ HID control point characteristic created");
    }

    /// <summary>
    /// Gets the combined HID report descriptor for keyboard and mouse.
    /// </summary>
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
}