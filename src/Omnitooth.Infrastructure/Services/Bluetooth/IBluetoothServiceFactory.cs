using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Omnitooth.Infrastructure.Services.Bluetooth;

/// <summary>
/// Factory interface for creating and validating Bluetooth GATT service providers.
/// Provides clean service creation with built-in health checks and validation.
/// </summary>
public interface IBluetoothServiceFactory
{
    /// <summary>
    /// Creates a new GATT service provider with HID service configuration.
    /// Includes validation and health checks before returning the service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A validated and healthy GattServiceProvider instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when service creation fails or validation errors occur.</exception>
    Task<GattServiceProvider> CreateHidServiceProviderAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates that a GATT service provider is in a healthy state for operation.
    /// </summary>
    /// <param name="serviceProvider">The service provider to validate.</param>
    /// <returns>True if the service provider is healthy and ready for use.</returns>
    bool ValidateServiceProvider(GattServiceProvider serviceProvider);
    
    /// <summary>
    /// Performs a comprehensive health check on a GATT service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider to check.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Health check result with detailed status information.</returns>
    Task<ServiceHealthResult> PerformHealthCheckAsync(GattServiceProvider serviceProvider, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Safely disposes a GATT service provider and cleans up all associated resources.
    /// </summary>
    /// <param name="serviceProvider">The service provider to dispose.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    Task DisposeServiceProviderAsync(GattServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a service health check operation.
/// </summary>
public record ServiceHealthResult
{
    /// <summary>
    /// Indicates whether the service is healthy and ready for operation.
    /// </summary>
    public bool IsHealthy { get; init; }
    
    /// <summary>
    /// Detailed status message describing the health check results.
    /// </summary>
    public string StatusMessage { get; init; } = string.Empty;
    
    /// <summary>
    /// List of any issues found during the health check.
    /// </summary>
    public List<string> Issues { get; init; } = new();
    
    /// <summary>
    /// Timestamp when the health check was performed.
    /// </summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Creates a healthy service result.
    /// </summary>
    public static ServiceHealthResult Healthy(string message = "Service is healthy and ready for operation") =>
        new() { IsHealthy = true, StatusMessage = message };
    
    /// <summary>
    /// Creates an unhealthy service result with issues.
    /// </summary>
    public static ServiceHealthResult Unhealthy(string message, params string[] issues) =>
        new() { IsHealthy = false, StatusMessage = message, Issues = issues.ToList() };
}