using Omnitooth.Core.Models;

namespace Omnitooth.Core.Interfaces;

/// <summary>
/// Interface for monitoring system health and performance metrics.
/// Provides real-time monitoring capabilities for Bluetooth operations and system resources.
/// </summary>
public interface IHealthMonitorService : IDisposable
{
    /// <summary>
    /// Gets an observable stream of health status updates.
    /// </summary>
    IObservable<HealthStatus> HealthStatusChanged { get; }
    
    /// <summary>
    /// Gets an observable stream of performance metrics updates.
    /// </summary>
    IObservable<PerformanceMetrics> MetricsUpdated { get; }
    
    /// <summary>
    /// Gets the current overall health status.
    /// </summary>
    HealthStatus CurrentHealth { get; }
    
    /// <summary>
    /// Gets the latest performance metrics snapshot.
    /// </summary>
    PerformanceMetrics CurrentMetrics { get; }
    
    /// <summary>
    /// Starts the health monitoring service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops the health monitoring service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopMonitoringAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Records a successful operation for metrics tracking.
    /// </summary>
    /// <param name="operationType">The type of operation that succeeded.</param>
    /// <param name="duration">The duration of the operation.</param>
    void RecordSuccess(string operationType, TimeSpan duration);
    
    /// <summary>
    /// Records a failed operation for metrics tracking.
    /// </summary>
    /// <param name="operationType">The type of operation that failed.</param>
    /// <param name="duration">The duration of the operation.</param>
    /// <param name="error">The error that occurred.</param>
    void RecordFailure(string operationType, TimeSpan duration, Exception error);
    
    /// <summary>
    /// Records a custom metric value.
    /// </summary>
    /// <param name="metricName">The name of the metric.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="tags">Optional tags for categorization.</param>
    void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null);
    
    /// <summary>
    /// Gets detailed health check results for a specific component.
    /// </summary>
    /// <param name="componentName">The name of the component to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Health check result for the specified component.</returns>
    Task<ComponentHealthResult> CheckComponentHealthAsync(string componentName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets comprehensive system health report.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Complete system health report.</returns>
    Task<SystemHealthReport> GetSystemHealthReportAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers a custom health check for a specific component.
    /// </summary>
    /// <param name="componentName">The name of the component.</param>
    /// <param name="healthCheck">The health check function.</param>
    void RegisterHealthCheck(string componentName, Func<CancellationToken, Task<ComponentHealthResult>> healthCheck);
    
    /// <summary>
    /// Unregisters a health check for a specific component.
    /// </summary>
    /// <param name="componentName">The name of the component.</param>
    void UnregisterHealthCheck(string componentName);
}