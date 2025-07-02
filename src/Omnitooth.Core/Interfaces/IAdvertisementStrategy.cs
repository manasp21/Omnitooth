using Omnitooth.Core.Models;

namespace Omnitooth.Core.Interfaces;

/// <summary>
/// Interface for advertisement optimization strategies.
/// Provides pluggable algorithms for optimizing advertisement parameters based on different goals and conditions.
/// </summary>
public interface IAdvertisementStrategy
{
    /// <summary>
    /// Gets the name of the strategy.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the description of what this strategy optimizes for.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Gets the strategy version.
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Gets whether this strategy supports the given optimization goals.
    /// </summary>
    /// <param name="goals">Optimization goals to check.</param>
    /// <returns>True if strategy supports these goals.</returns>
    bool SupportsGoals(AdvertisementOptimizationGoals goals);
    
    /// <summary>
    /// Calculates optimal advertisement configuration based on current metrics and goals.
    /// </summary>
    /// <param name="currentConfiguration">Current advertisement configuration.</param>
    /// <param name="currentMetrics">Current performance metrics.</param>
    /// <param name="optimizationGoals">Optimization goals and constraints.</param>
    /// <param name="environmentalData">Environmental data for optimization context.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Optimized configuration and recommendations.</returns>
    Task<AdvertisementOptimizationResult> OptimizeAsync(
        AdvertisementConfiguration currentConfiguration,
        AdvertisementMetrics currentMetrics,
        AdvertisementOptimizationGoals optimizationGoals,
        Dictionary<string, object> environmentalData,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates that the strategy can be applied with the given parameters.
    /// </summary>
    /// <param name="configuration">Configuration to validate.</param>
    /// <param name="goals">Optimization goals.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Validation result indicating if strategy can be applied.</returns>
    Task<ValidationResult> ValidateAsync(
        AdvertisementConfiguration configuration,
        AdvertisementOptimizationGoals goals,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets strategy-specific configuration parameters that can be tuned.
    /// </summary>
    /// <returns>Dictionary of parameter names and their default values.</returns>
    Dictionary<string, object> GetConfigurableParameters();
    
    /// <summary>
    /// Updates strategy-specific configuration parameters.
    /// </summary>
    /// <param name="parameters">Parameters to update.</param>
    /// <returns>True if parameters were successfully updated.</returns>
    bool UpdateParameters(Dictionary<string, object> parameters);
    
    /// <summary>
    /// Gets performance metrics for this strategy's effectiveness.
    /// </summary>
    /// <returns>Strategy performance metrics.</returns>
    StrategyPerformanceMetrics GetPerformanceMetrics();
    
    /// <summary>
    /// Resets the strategy's internal state and performance metrics.
    /// </summary>
    void Reset();
}

/// <summary>
/// Represents performance metrics for an advertisement strategy.
/// </summary>
public record StrategyPerformanceMetrics
{
    /// <summary>
    /// Number of times the strategy has been applied.
    /// </summary>
    public long ApplicationCount { get; init; }
    
    /// <summary>
    /// Number of successful optimizations.
    /// </summary>
    public long SuccessfulOptimizations { get; init; }
    
    /// <summary>
    /// Success rate percentage (0-100).
    /// </summary>
    public double SuccessRate { get; init; }
    
    /// <summary>
    /// Average performance improvement achieved.
    /// </summary>
    public double AverageImprovement { get; init; }
    
    /// <summary>
    /// Average execution time for optimization.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; init; }
    
    /// <summary>
    /// Confidence score in strategy's effectiveness (0-100).
    /// </summary>
    public double ConfidenceScore { get; init; }
    
    /// <summary>
    /// Last time the strategy was successfully applied.
    /// </summary>
    public DateTime? LastSuccessfulApplication { get; init; }
    
    /// <summary>
    /// Strategy-specific performance data.
    /// </summary>
    public Dictionary<string, double> CustomMetrics { get; init; } = new();
}