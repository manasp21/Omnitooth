using Omnitooth.Core.Models;

namespace Omnitooth.Core.Interfaces;

/// <summary>
/// Interface for advanced Bluetooth advertisement management with dynamic parameter control and power optimization.
/// Provides sophisticated advertisement lifecycle control, conflict resolution, and scheduling capabilities.
/// </summary>
public interface IAdvertisementManager : IDisposable
{
    /// <summary>
    /// Gets an observable stream of advertisement status changes.
    /// </summary>
    IObservable<AdvertisementStatusEvent> StatusChanged { get; }
    
    /// <summary>
    /// Gets an observable stream of advertisement performance metrics.
    /// </summary>
    IObservable<AdvertisementMetrics> MetricsUpdated { get; }
    
    /// <summary>
    /// Gets the current advertisement status.
    /// </summary>
    AdvertisementStatus CurrentStatus { get; }
    
    /// <summary>
    /// Gets the current advertisement configuration.
    /// </summary>
    AdvertisementConfiguration CurrentConfiguration { get; }
    
    /// <summary>
    /// Gets the latest advertisement metrics.
    /// </summary>
    AdvertisementMetrics CurrentMetrics { get; }
    
    /// <summary>
    /// Starts advertisement with specified configuration.
    /// </summary>
    /// <param name="configuration">Advertisement configuration parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Advertisement start result.</returns>
    Task<AdvertisementResult> StartAdvertisementAsync(AdvertisementConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops the current advertisement.
    /// </summary>
    /// <param name="reason">Reason for stopping advertisement.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Advertisement stop result.</returns>
    Task<AdvertisementResult> StopAdvertisementAsync(string reason = "Manual stop", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates advertisement configuration dynamically without stopping.
    /// </summary>
    /// <param name="configuration">New configuration parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Configuration update result.</returns>
    Task<AdvertisementResult> UpdateConfigurationAsync(AdvertisementConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Optimizes advertisement parameters based on current conditions.
    /// </summary>
    /// <param name="optimizationGoals">Optimization objectives (power, performance, range, etc.).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Optimization result with recommended settings.</returns>
    Task<AdvertisementOptimizationResult> OptimizeParametersAsync(AdvertisementOptimizationGoals optimizationGoals, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Schedules advertisement with power management and conflict resolution.
    /// </summary>
    /// <param name="schedule">Advertisement schedule configuration.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Scheduling result.</returns>
    Task<AdvertisementScheduleResult> ScheduleAdvertisementAsync(AdvertisementSchedule schedule, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Detects and resolves advertisement conflicts with other Bluetooth applications.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Conflict resolution result.</returns>
    Task<ConflictResolutionResult> ResolveConflictsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Monitors advertisement performance and adjusts parameters automatically.
    /// </summary>
    /// <param name="monitoringConfiguration">Performance monitoring settings.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the monitoring operation.</returns>
    Task StartPerformanceMonitoringAsync(PerformanceMonitoringConfiguration monitoringConfiguration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops performance monitoring.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the stop operation.</returns>
    Task StopPerformanceMonitoringAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets comprehensive advertisement diagnostics and health information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Detailed advertisement diagnostics.</returns>
    Task<AdvertisementDiagnostics> GetDiagnosticsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates advertisement configuration for compliance and optimization.
    /// </summary>
    /// <param name="configuration">Configuration to validate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Configuration validation result.</returns>
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(AdvertisementConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets available advertisement channels and their utilization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Channel analysis result.</returns>
    Task<ChannelAnalysisResult> AnalyzeChannelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers a custom advertisement strategy.
    /// </summary>
    /// <param name="strategyName">Name of the strategy.</param>
    /// <param name="strategy">Advertisement strategy implementation.</param>
    void RegisterStrategy(string strategyName, IAdvertisementStrategy strategy);
    
    /// <summary>
    /// Unregisters a custom advertisement strategy.
    /// </summary>
    /// <param name="strategyName">Name of the strategy to remove.</param>
    void UnregisterStrategy(string strategyName);
    
    /// <summary>
    /// Applies a registered advertisement strategy.
    /// </summary>
    /// <param name="strategyName">Name of the strategy to apply.</param>
    /// <param name="parameters">Strategy-specific parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Strategy application result.</returns>
    Task<StrategyApplicationResult> ApplyStrategyAsync(string strategyName, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
}