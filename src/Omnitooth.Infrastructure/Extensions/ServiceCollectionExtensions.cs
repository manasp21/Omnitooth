using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Omnitooth.Core.Configuration;
using Omnitooth.Core.Interfaces;
using Omnitooth.Infrastructure.Services.Input;
using Omnitooth.Infrastructure.Services.Hid;
using Omnitooth.Infrastructure.Services.Bluetooth;
using Omnitooth.Infrastructure.Services.Monitoring;
using Omnitooth.Infrastructure.Services.Compliance;

namespace Omnitooth.Infrastructure.Extensions;

/// <summary>
/// Extension methods for service collection configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<InputConfiguration>(configuration.GetSection("Omnitooth:Input"));
        services.Configure<HidConfiguration>(configuration.GetSection("Omnitooth:Hid"));
        services.Configure<BluetoothConfiguration>(configuration.GetSection("Omnitooth:Bluetooth"));
        
        // Register input capture services
        services.AddSingleton<IInputCaptureService, CompositeInputCaptureService>();
        
        // Register HID services
        services.AddSingleton<IHidService, HidReportBuilderService>();
        
        // Register Bluetooth services
        services.AddSingleton<IBluetoothServiceFactory, BluetoothServiceFactory>();
        
        // Configure and register circuit breaker
        services.Configure<CircuitBreakerOptions>(options =>
        {
            var bluetoothConfig = configuration.GetSection("Omnitooth:Bluetooth");
            options.FailureThreshold = bluetoothConfig.GetValue<int>("CircuitBreakerFailureThreshold", 5);
            options.RecoveryTimeout = TimeSpan.FromSeconds(bluetoothConfig.GetValue<int>("CircuitBreakerRecoveryTimeoutSeconds", 30));
            options.HalfOpenMaxAttempts = bluetoothConfig.GetValue<int>("CircuitBreakerHalfOpenMaxAttempts", 3);
        });
        services.AddSingleton<ICircuitBreaker, BluetoothCircuitBreaker>();
        
        // Register health monitoring services
        services.AddSingleton<IHealthMonitorService, BluetoothHealthMonitor>();
        
        // Register compliance management services
        services.AddSingleton<IBluetoothComplianceManager, BluetoothApiCompatibilityLayer>();
        
        // Register main Bluetooth service (depends on all above services)
        services.AddSingleton<IBluetoothService, BluetoothGattServerService>();
        
        return services;
    }
}