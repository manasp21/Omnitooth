namespace Omnitooth.Core.Models;

/// <summary>
/// Represents the overall health status of the system.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// System is healthy and operating normally.
    /// </summary>
    Healthy,
    
    /// <summary>
    /// System is degraded but still functional.
    /// </summary>
    Degraded,
    
    /// <summary>
    /// System is experiencing critical issues.
    /// </summary>
    Critical,
    
    /// <summary>
    /// System is offline or not responding.
    /// </summary>
    Offline
}

/// <summary>
/// Represents performance metrics for system monitoring.
/// </summary>
public record PerformanceMetrics
{
    /// <summary>
    /// Total number of operations performed.
    /// </summary>
    public long TotalOperations { get; init; }
    
    /// <summary>
    /// Number of successful operations.
    /// </summary>
    public long SuccessfulOperations { get; init; }
    
    /// <summary>
    /// Number of failed operations.
    /// </summary>
    public long FailedOperations { get; init; }
    
    /// <summary>
    /// Success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate { get; init; }
    
    /// <summary>
    /// Average operation duration in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; init; }
    
    /// <summary>
    /// 95th percentile operation duration in milliseconds.
    /// </summary>
    public double P95LatencyMs { get; init; }
    
    /// <summary>
    /// Operations per second throughput.
    /// </summary>
    public double OperationsPerSecond { get; init; }
    
    /// <summary>
    /// Memory usage in bytes.
    /// </summary>
    public long MemoryUsageBytes { get; init; }
    
    /// <summary>
    /// CPU usage percentage (0-100).
    /// </summary>
    public double CpuUsagePercent { get; init; }
    
    /// <summary>
    /// Number of active connections.
    /// </summary>
    public int ActiveConnections { get; init; }
    
    /// <summary>
    /// Bluetooth adapter status.
    /// </summary>
    public string BluetoothAdapterStatus { get; init; } = string.Empty;
    
    /// <summary>
    /// Circuit breaker status.
    /// </summary>
    public string CircuitBreakerStatus { get; init; } = string.Empty;
    
    /// <summary>
    /// Timestamp when metrics were collected.
    /// </summary>
    public DateTime CollectedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Additional custom metrics.
    /// </summary>
    public Dictionary<string, double> CustomMetrics { get; init; } = new();
}

/// <summary>
/// Represents health check result for a specific component.
/// </summary>
public record ComponentHealthResult
{
    /// <summary>
    /// Name of the component.
    /// </summary>
    public string ComponentName { get; init; } = string.Empty;
    
    /// <summary>
    /// Health status of the component.
    /// </summary>
    public HealthStatus Status { get; init; }
    
    /// <summary>
    /// Human-readable description of the health status.
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Time taken to perform the health check.
    /// </summary>
    public TimeSpan CheckDuration { get; init; }
    
    /// <summary>
    /// Timestamp when the health check was performed.
    /// </summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Exception that occurred during health check, if any.
    /// </summary>
    public Exception? Exception { get; init; }
    
    /// <summary>
    /// Additional diagnostic data.
    /// </summary>
    public Dictionary<string, object> DiagnosticData { get; init; } = new();
    
    /// <summary>
    /// Recommended actions to resolve issues.
    /// </summary>
    public List<string> RecommendedActions { get; init; } = new();
}

/// <summary>
/// Represents a comprehensive system health report.
/// </summary>
public record SystemHealthReport
{
    /// <summary>
    /// Overall system health status.
    /// </summary>
    public HealthStatus OverallStatus { get; init; }
    
    /// <summary>
    /// Current performance metrics.
    /// </summary>
    public PerformanceMetrics Metrics { get; init; } = new();
    
    /// <summary>
    /// Health results for all monitored components.
    /// </summary>
    public Dictionary<string, ComponentHealthResult> ComponentResults { get; init; } = new();
    
    /// <summary>
    /// Summary of critical issues found.
    /// </summary>
    public List<string> CriticalIssues { get; init; } = new();
    
    /// <summary>
    /// Summary of warnings found.
    /// </summary>
    public List<string> Warnings { get; init; } = new();
    
    /// <summary>
    /// Recommended actions to improve system health.
    /// </summary>
    public List<string> Recommendations { get; init; } = new();
    
    /// <summary>
    /// Timestamp when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Duration of the complete health assessment.
    /// </summary>
    public TimeSpan AssessmentDuration { get; init; }
}

/// <summary>
/// Represents an operation metric record.
/// </summary>
public record OperationMetric
{
    /// <summary>
    /// Type of operation.
    /// </summary>
    public string OperationType { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// Duration of the operation.
    /// </summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>
    /// Timestamp when the operation occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Error information if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Additional context data.
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
}

/// <summary>
/// Represents a custom metric value with metadata.
/// </summary>
public record CustomMetric
{
    /// <summary>
    /// Name of the metric.
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Numeric value of the metric.
    /// </summary>
    public double Value { get; init; }
    
    /// <summary>
    /// Timestamp when the metric was recorded.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Optional tags for categorization and filtering.
    /// </summary>
    public Dictionary<string, string> Tags { get; init; } = new();
    
    /// <summary>
    /// Unit of measurement for the metric.
    /// </summary>
    public string Unit { get; init; } = string.Empty;
}