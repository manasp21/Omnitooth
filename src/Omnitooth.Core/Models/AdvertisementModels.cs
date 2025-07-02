using System.Collections.Concurrent;

namespace Omnitooth.Core.Models;

/// <summary>
/// Represents the current status of advertisement operations.
/// </summary>
public enum AdvertisementStatus
{
    /// <summary>
    /// Advertisement is stopped.
    /// </summary>
    Stopped,
    
    /// <summary>
    /// Advertisement is starting up.
    /// </summary>
    Starting,
    
    /// <summary>
    /// Advertisement is active and running.
    /// </summary>
    Active,
    
    /// <summary>
    /// Advertisement is stopping.
    /// </summary>
    Stopping,
    
    /// <summary>
    /// Advertisement failed due to error.
    /// </summary>
    Failed,
    
    /// <summary>
    /// Advertisement is paused/suspended.
    /// </summary>
    Suspended
}

/// <summary>
/// Represents an advertisement status change event.
/// </summary>
public record AdvertisementStatusEvent
{
    /// <summary>
    /// Previous advertisement status.
    /// </summary>
    public AdvertisementStatus PreviousStatus { get; init; }
    
    /// <summary>
    /// New advertisement status.
    /// </summary>
    public AdvertisementStatus NewStatus { get; init; }
    
    /// <summary>
    /// Reason for the status change.
    /// </summary>
    public string Reason { get; init; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the status change occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Duration in the previous status.
    /// </summary>
    public TimeSpan DurationInPreviousStatus { get; init; }
    
    /// <summary>
    /// Additional context data about the status change.
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
}

/// <summary>
/// Represents advertisement performance metrics.
/// </summary>
public record AdvertisementMetrics
{
    /// <summary>
    /// Number of advertisement packets sent.
    /// </summary>
    public long PacketsSent { get; init; }
    
    /// <summary>
    /// Number of advertisement packets that failed to send.
    /// </summary>
    public long PacketsFailed { get; init; }
    
    /// <summary>
    /// Success rate percentage (0-100).
    /// </summary>
    public double SuccessRate { get; init; }
    
    /// <summary>
    /// Current advertisement interval in milliseconds.
    /// </summary>
    public int IntervalMs { get; init; }
    
    /// <summary>
    /// Average transmission power in dBm.
    /// </summary>
    public double TransmissionPowerDbm { get; init; }
    
    /// <summary>
    /// Number of devices currently scanning.
    /// </summary>
    public int ScanningDevices { get; init; }
    
    /// <summary>
    /// Number of successful connections established.
    /// </summary>
    public int ConnectionsEstablished { get; init; }
    
    /// <summary>
    /// Average connection establishment time.
    /// </summary>
    public TimeSpan AverageConnectionTime { get; init; }
    
    /// <summary>
    /// Channel utilization percentage (0-100).
    /// </summary>
    public double ChannelUtilization { get; init; }
    
    /// <summary>
    /// Interference level (0-100, higher is worse).
    /// </summary>
    public double InterferenceLevel { get; init; }
    
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
/// Represents advertisement configuration parameters.
/// </summary>
public record AdvertisementConfiguration
{
    /// <summary>
    /// Device name to advertise.
    /// </summary>
    public string DeviceName { get; init; } = "Omnitooth HID";
    
    /// <summary>
    /// Advertisement interval in milliseconds.
    /// </summary>
    public int IntervalMs { get; init; } = 100;
    
    /// <summary>
    /// Transmission power level in dBm.
    /// </summary>
    public int TransmissionPowerDbm { get; init; } = 0;
    
    /// <summary>
    /// Whether the device is discoverable.
    /// </summary>
    public bool IsDiscoverable { get; init; } = true;
    
    /// <summary>
    /// Whether the device is connectable.
    /// </summary>
    public bool IsConnectable { get; init; } = true;
    
    /// <summary>
    /// Advertisement timeout in milliseconds (0 for infinite).
    /// </summary>
    public int TimeoutMs { get; init; } = 0;
    
    /// <summary>
    /// Services to include in advertisement.
    /// </summary>
    public List<Guid> ServiceUuids { get; init; } = new();
    
    /// <summary>
    /// Manufacturer-specific data.
    /// </summary>
    public Dictionary<ushort, byte[]> ManufacturerData { get; init; } = new();
    
    /// <summary>
    /// Service data to include.
    /// </summary>
    public Dictionary<Guid, byte[]> ServiceData { get; init; } = new();
    
    /// <summary>
    /// Local name to advertise.
    /// </summary>
    public string? LocalName { get; init; }
    
    /// <summary>
    /// Whether to use extended advertising features.
    /// </summary>
    public bool UseExtendedAdvertising { get; init; } = false;
    
    /// <summary>
    /// Preferred PHY for advertising (1M, 2M, Coded).
    /// </summary>
    public AdvertisementPhy PreferredPhy { get; init; } = AdvertisementPhy.Le1M;
    
    /// <summary>
    /// Secondary PHY for extended advertising.
    /// </summary>
    public AdvertisementPhy SecondaryPhy { get; init; } = AdvertisementPhy.Le1M;
    
    /// <summary>
    /// Additional configuration properties.
    /// </summary>
    public Dictionary<string, object> ExtendedProperties { get; init; } = new();
}

/// <summary>
/// Represents advertisement PHY options.
/// </summary>
public enum AdvertisementPhy
{
    /// <summary>
    /// 1M PHY for standard range and power consumption.
    /// </summary>
    Le1M,
    
    /// <summary>
    /// 2M PHY for higher data rates.
    /// </summary>
    Le2M,
    
    /// <summary>
    /// Coded PHY for extended range.
    /// </summary>
    Coded
}

/// <summary>
/// Represents the result of an advertisement operation.
/// </summary>
public record AdvertisementResult
{
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// New advertisement status after operation.
    /// </summary>
    public AdvertisementStatus Status { get; init; }
    
    /// <summary>
    /// Duration of the operation.
    /// </summary>
    public TimeSpan OperationDuration { get; init; }
    
    /// <summary>
    /// Timestamp when operation completed.
    /// </summary>
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Additional result data.
    /// </summary>
    public Dictionary<string, object> ResultData { get; init; } = new();
    
    /// <summary>
    /// Warnings encountered during operation.
    /// </summary>
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// Represents advertisement optimization goals.
/// </summary>
public record AdvertisementOptimizationGoals
{
    /// <summary>
    /// Whether to optimize for power consumption.
    /// </summary>
    public bool OptimizeForPower { get; init; } = false;
    
    /// <summary>
    /// Whether to optimize for maximum range.
    /// </summary>
    public bool OptimizeForRange { get; init; } = false;
    
    /// <summary>
    /// Whether to optimize for connection speed.
    /// </summary>
    public bool OptimizeForConnectionSpeed { get; init; } = true;
    
    /// <summary>
    /// Whether to optimize for reliability.
    /// </summary>
    public bool OptimizeForReliability { get; init; } = true;
    
    /// <summary>
    /// Whether to minimize interference.
    /// </summary>
    public bool MinimizeInterference { get; init; } = true;
    
    /// <summary>
    /// Target success rate percentage (0-100).
    /// </summary>
    public double TargetSuccessRate { get; init; } = 95.0;
    
    /// <summary>
    /// Maximum acceptable latency in milliseconds.
    /// </summary>
    public int MaxLatencyMs { get; init; } = 50;
    
    /// <summary>
    /// Priority weights for different optimization aspects.
    /// </summary>
    public Dictionary<string, double> OptimizationWeights { get; init; } = new();
}

/// <summary>
/// Represents advertisement optimization result.
/// </summary>
public record AdvertisementOptimizationResult
{
    /// <summary>
    /// Whether optimization was successful.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// Optimized advertisement configuration.
    /// </summary>
    public AdvertisementConfiguration OptimizedConfiguration { get; init; } = new();
    
    /// <summary>
    /// Expected performance improvements.
    /// </summary>
    public Dictionary<string, double> ExpectedImprovements { get; init; } = new();
    
    /// <summary>
    /// Optimization recommendations.
    /// </summary>
    public List<string> Recommendations { get; init; } = new();
    
    /// <summary>
    /// Confidence score of optimization (0-100).
    /// </summary>
    public double ConfidenceScore { get; init; }
    
    /// <summary>
    /// Optimization algorithm used.
    /// </summary>
    public string OptimizationAlgorithm { get; init; } = string.Empty;
    
    /// <summary>
    /// Time taken to perform optimization.
    /// </summary>
    public TimeSpan OptimizationDuration { get; init; }
}

/// <summary>
/// Represents advertisement schedule configuration.
/// </summary>
public record AdvertisementSchedule
{
    /// <summary>
    /// Whether scheduling is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = false;
    
    /// <summary>
    /// Schedule start time.
    /// </summary>
    public DateTime StartTime { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Schedule end time (null for indefinite).
    /// </summary>
    public DateTime? EndTime { get; init; }
    
    /// <summary>
    /// Recurrence pattern for scheduled advertising.
    /// </summary>
    public ScheduleRecurrence Recurrence { get; init; } = ScheduleRecurrence.None;
    
    /// <summary>
    /// Days of the week when advertising should be active.
    /// </summary>
    public DayOfWeek[] ActiveDays { get; init; } = Array.Empty<DayOfWeek>();
    
    /// <summary>
    /// Time ranges during the day when advertising should be active.
    /// </summary>
    public List<TimeRange> ActiveTimeRanges { get; init; } = new();
    
    /// <summary>
    /// Power management settings for scheduled advertising.
    /// </summary>
    public PowerManagementSettings PowerSettings { get; init; } = new();
    
    /// <summary>
    /// Priority level for this schedule.
    /// </summary>
    public SchedulePriority Priority { get; init; } = SchedulePriority.Normal;
}

/// <summary>
/// Represents schedule recurrence patterns.
/// </summary>
public enum ScheduleRecurrence
{
    /// <summary>
    /// No recurrence, one-time schedule.
    /// </summary>
    None,
    
    /// <summary>
    /// Repeat daily.
    /// </summary>
    Daily,
    
    /// <summary>
    /// Repeat weekly.
    /// </summary>
    Weekly,
    
    /// <summary>
    /// Repeat monthly.
    /// </summary>
    Monthly,
    
    /// <summary>
    /// Custom recurrence pattern.
    /// </summary>
    Custom
}

/// <summary>
/// Represents a time range during the day.
/// </summary>
public record TimeRange
{
    /// <summary>
    /// Start time of the range.
    /// </summary>
    public TimeOnly Start { get; init; }
    
    /// <summary>
    /// End time of the range.
    /// </summary>
    public TimeOnly End { get; init; }
    
    /// <summary>
    /// Whether the range spans midnight.
    /// </summary>
    public bool SpansMidnight => End < Start;
}

/// <summary>
/// Represents power management settings.
/// </summary>
public record PowerManagementSettings
{
    /// <summary>
    /// Whether power management is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;
    
    /// <summary>
    /// Power mode for advertising.
    /// </summary>
    public PowerMode PowerMode { get; init; } = PowerMode.Balanced;
    
    /// <summary>
    /// Maximum power consumption in watts.
    /// </summary>
    public double MaxPowerConsumptionWatts { get; init; } = 1.0;
    
    /// <summary>
    /// Battery level threshold below which to reduce power (0-100).
    /// </summary>
    public int LowBatteryThreshold { get; init; } = 20;
    
    /// <summary>
    /// Whether to automatically adjust power based on conditions.
    /// </summary>
    public bool AutoAdjustPower { get; init; } = true;
}

/// <summary>
/// Represents power management modes.
/// </summary>
public enum PowerMode
{
    /// <summary>
    /// Maximum performance, highest power consumption.
    /// </summary>
    HighPerformance,
    
    /// <summary>
    /// Balanced performance and power consumption.
    /// </summary>
    Balanced,
    
    /// <summary>
    /// Power saving mode with reduced performance.
    /// </summary>
    PowerSaver,
    
    /// <summary>
    /// Ultra-low power mode for maximum battery life.
    /// </summary>
    UltraLowPower
}

/// <summary>
/// Represents schedule priority levels.
/// </summary>
public enum SchedulePriority
{
    /// <summary>
    /// Low priority schedule.
    /// </summary>
    Low,
    
    /// <summary>
    /// Normal priority schedule.
    /// </summary>
    Normal,
    
    /// <summary>
    /// High priority schedule.
    /// </summary>
    High,
    
    /// <summary>
    /// Critical priority schedule.
    /// </summary>
    Critical
}

/// <summary>
/// Represents advertisement schedule operation result.
/// </summary>
public record AdvertisementScheduleResult
{
    /// <summary>
    /// Whether scheduling was successful.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// Error message if scheduling failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Scheduled advertisement ID.
    /// </summary>
    public string ScheduleId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Next scheduled execution time.
    /// </summary>
    public DateTime? NextExecutionTime { get; init; }
    
    /// <summary>
    /// Conflicts detected with existing schedules.
    /// </summary>
    public List<ScheduleConflict> Conflicts { get; init; } = new();
    
    /// <summary>
    /// Recommendations for schedule optimization.
    /// </summary>
    public List<string> Recommendations { get; init; } = new();
}

/// <summary>
/// Represents a scheduling conflict.
/// </summary>
public record ScheduleConflict
{
    /// <summary>
    /// ID of the conflicting schedule.
    /// </summary>
    public string ConflictingScheduleId { get; init; } = string.Empty;
    
    /// <summary>
    /// Description of the conflict.
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Severity of the conflict.
    /// </summary>
    public ConflictSeverity Severity { get; init; }
    
    /// <summary>
    /// Suggested resolution for the conflict.
    /// </summary>
    public string SuggestedResolution { get; init; } = string.Empty;
}

/// <summary>
/// Represents conflict severity levels.
/// </summary>
public enum ConflictSeverity
{
    /// <summary>
    /// Minor conflict that doesn't prevent operation.
    /// </summary>
    Minor,
    
    /// <summary>
    /// Moderate conflict that may impact performance.
    /// </summary>
    Moderate,
    
    /// <summary>
    /// Major conflict that significantly impacts operation.
    /// </summary>
    Major,
    
    /// <summary>
    /// Critical conflict that prevents operation.
    /// </summary>
    Critical
}

/// <summary>
/// Represents conflict resolution result.
/// </summary>
public record ConflictResolutionResult
{
    /// <summary>
    /// Whether conflict resolution was successful.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// Conflicts that were successfully resolved.
    /// </summary>
    public List<AdvertisementConflict> ResolvedConflicts { get; init; } = new();
    
    /// <summary>
    /// Conflicts that could not be resolved.
    /// </summary>
    public List<AdvertisementConflict> UnresolvedConflicts { get; init; } = new();
    
    /// <summary>
    /// Actions taken to resolve conflicts.
    /// </summary>
    public List<ConflictResolutionAction> ActionsTaken { get; init; } = new();
    
    /// <summary>
    /// Recommendations for preventing future conflicts.
    /// </summary>
    public List<string> PreventionRecommendations { get; init; } = new();
}

/// <summary>
/// Represents an advertisement conflict with other applications.
/// </summary>
public record AdvertisementConflict
{
    /// <summary>
    /// Application causing the conflict.
    /// </summary>
    public string ConflictingApplication { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of conflict.
    /// </summary>
    public ConflictType ConflictType { get; init; }
    
    /// <summary>
    /// Description of the conflict.
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Impact assessment of the conflict.
    /// </summary>
    public ConflictImpact Impact { get; init; }
    
    /// <summary>
    /// Detected at timestamp.
    /// </summary>
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents types of advertisement conflicts.
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// Channel interference conflict.
    /// </summary>
    ChannelInterference,
    
    /// <summary>
    /// Resource contention conflict.
    /// </summary>
    ResourceContention,
    
    /// <summary>
    /// Advertisement collision conflict.
    /// </summary>
    AdvertisementCollision,
    
    /// <summary>
    /// Power management conflict.
    /// </summary>
    PowerManagement,
    
    /// <summary>
    /// Protocol violation conflict.
    /// </summary>
    ProtocolViolation
}

/// <summary>
/// Represents conflict impact levels.
/// </summary>
public enum ConflictImpact
{
    /// <summary>
    /// No significant impact.
    /// </summary>
    None,
    
    /// <summary>
    /// Minor performance degradation.
    /// </summary>
    Minor,
    
    /// <summary>
    /// Moderate performance impact.
    /// </summary>
    Moderate,
    
    /// <summary>
    /// Severe performance impact.
    /// </summary>
    Severe,
    
    /// <summary>
    /// Complete service disruption.
    /// </summary>
    Critical
}

/// <summary>
/// Represents an action taken to resolve a conflict.
/// </summary>
public record ConflictResolutionAction
{
    /// <summary>
    /// Description of the action taken.
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether the action was successful.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// Timestamp when action was performed.
    /// </summary>
    public DateTime PerformedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Result of the action.
    /// </summary>
    public string Result { get; init; } = string.Empty;
}

/// <summary>
/// Represents performance monitoring configuration.
/// </summary>
public record PerformanceMonitoringConfiguration
{
    /// <summary>
    /// Whether performance monitoring is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;
    
    /// <summary>
    /// Monitoring interval in milliseconds.
    /// </summary>
    public int MonitoringIntervalMs { get; init; } = 5000;
    
    /// <summary>
    /// Metrics to monitor.
    /// </summary>
    public List<string> MetricsToMonitor { get; init; } = new();
    
    /// <summary>
    /// Performance thresholds for alerting.
    /// </summary>
    public Dictionary<string, double> PerformanceThresholds { get; init; } = new();
    
    /// <summary>
    /// Whether to automatically adjust parameters based on performance.
    /// </summary>
    public bool AutoAdjustParameters { get; init; } = true;
    
    /// <summary>
    /// Maximum history to retain for performance data.
    /// </summary>
    public TimeSpan MaxHistoryRetention { get; init; } = TimeSpan.FromHours(24);
}

/// <summary>
/// Represents comprehensive advertisement diagnostics.
/// </summary>
public record AdvertisementDiagnostics
{
    /// <summary>
    /// Overall health status of advertisement.
    /// </summary>
    public AdvertisementHealth OverallHealth { get; init; }
    
    /// <summary>
    /// Current advertisement metrics.
    /// </summary>
    public AdvertisementMetrics CurrentMetrics { get; init; } = new();
    
    /// <summary>
    /// Detected issues and problems.
    /// </summary>
    public List<AdvertisementIssue> Issues { get; init; } = new();
    
    /// <summary>
    /// Performance recommendations.
    /// </summary>
    public List<string> Recommendations { get; init; } = new();
    
    /// <summary>
    /// System resource utilization.
    /// </summary>
    public Dictionary<string, double> ResourceUtilization { get; init; } = new();
    
    /// <summary>
    /// Environmental factors affecting performance.
    /// </summary>
    public Dictionary<string, object> EnvironmentalFactors { get; init; } = new();
    
    /// <summary>
    /// Timestamp when diagnostics were collected.
    /// </summary>
    public DateTime CollectedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents advertisement health status.
/// </summary>
public enum AdvertisementHealth
{
    /// <summary>
    /// Advertisement is healthy and operating optimally.
    /// </summary>
    Excellent,
    
    /// <summary>
    /// Advertisement is healthy with minor issues.
    /// </summary>
    Good,
    
    /// <summary>
    /// Advertisement has moderate issues affecting performance.
    /// </summary>
    Fair,
    
    /// <summary>
    /// Advertisement has significant issues.
    /// </summary>
    Poor,
    
    /// <summary>
    /// Advertisement is critically impaired.
    /// </summary>
    Critical
}

/// <summary>
/// Represents an advertisement issue or problem.
/// </summary>
public record AdvertisementIssue
{
    /// <summary>
    /// Severity of the issue.
    /// </summary>
    public IssueSeverity Severity { get; init; }
    
    /// <summary>
    /// Category of the issue.
    /// </summary>
    public string Category { get; init; } = string.Empty;
    
    /// <summary>
    /// Description of the issue.
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Recommended actions to resolve the issue.
    /// </summary>
    public List<string> RecommendedActions { get; init; } = new();
    
    /// <summary>
    /// When the issue was first detected.
    /// </summary>
    public DateTime FirstDetected { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Impact of the issue on performance.
    /// </summary>
    public double PerformanceImpact { get; init; }
}

/// <summary>
/// Represents configuration validation result.
/// </summary>
public record ConfigurationValidationResult
{
    /// <summary>
    /// Whether the configuration is valid.
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// Configuration validation errors.
    /// </summary>
    public List<string> ValidationErrors { get; init; } = new();
    
    /// <summary>
    /// Configuration warnings.
    /// </summary>
    public List<string> Warnings { get; init; } = new();
    
    /// <summary>
    /// Optimization suggestions for the configuration.
    /// </summary>
    public List<string> OptimizationSuggestions { get; init; } = new();
    
    /// <summary>
    /// Validation score (0-100).
    /// </summary>
    public double ValidationScore { get; init; }
    
    /// <summary>
    /// Validated configuration with corrections applied.
    /// </summary>
    public AdvertisementConfiguration ValidatedConfiguration { get; init; } = new();
}

/// <summary>
/// Represents channel analysis result.
/// </summary>
public record ChannelAnalysisResult
{
    /// <summary>
    /// Whether channel analysis was successful.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// Available channels and their properties.
    /// </summary>
    public List<ChannelInfo> AvailableChannels { get; init; } = new();
    
    /// <summary>
    /// Recommended channels for optimal performance.
    /// </summary>
    public List<int> RecommendedChannels { get; init; } = new();
    
    /// <summary>
    /// Channels to avoid due to interference.
    /// </summary>
    public List<int> ChannelsToAvoid { get; init; } = new();
    
    /// <summary>
    /// Overall channel environment assessment.
    /// </summary>
    public ChannelEnvironment Environment { get; init; }
    
    /// <summary>
    /// Analysis timestamp.
    /// </summary>
    public DateTime AnalyzedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents information about a specific channel.
/// </summary>
public record ChannelInfo
{
    /// <summary>
    /// Channel number.
    /// </summary>
    public int ChannelNumber { get; init; }
    
    /// <summary>
    /// Channel frequency in MHz.
    /// </summary>
    public double FrequencyMHz { get; init; }
    
    /// <summary>
    /// Channel utilization percentage (0-100).
    /// </summary>
    public double Utilization { get; init; }
    
    /// <summary>
    /// Interference level (0-100).
    /// </summary>
    public double InterferenceLevel { get; init; }
    
    /// <summary>
    /// Signal quality score (0-100).
    /// </summary>
    public double SignalQuality { get; init; }
    
    /// <summary>
    /// Whether the channel is currently in use.
    /// </summary>
    public bool IsInUse { get; init; }
    
    /// <summary>
    /// Other devices detected on this channel.
    /// </summary>
    public List<string> DetectedDevices { get; init; } = new();
}

/// <summary>
/// Represents channel environment assessment.
/// </summary>
public enum ChannelEnvironment
{
    /// <summary>
    /// Clean environment with minimal interference.
    /// </summary>
    Clean,
    
    /// <summary>
    /// Moderate interference environment.
    /// </summary>
    Moderate,
    
    /// <summary>
    /// Noisy environment with significant interference.
    /// </summary>
    Noisy,
    
    /// <summary>
    /// Heavily congested environment.
    /// </summary>
    Congested,
    
    /// <summary>
    /// Hostile environment with severe interference.
    /// </summary>
    Hostile
}

/// <summary>
/// Represents strategy application result.
/// </summary>
public record StrategyApplicationResult
{
    /// <summary>
    /// Whether strategy application was successful.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// Name of the applied strategy.
    /// </summary>
    public string StrategyName { get; init; } = string.Empty;
    
    /// <summary>
    /// Error message if application failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Configuration changes made by the strategy.
    /// </summary>
    public Dictionary<string, object> ConfigurationChanges { get; init; } = new();
    
    /// <summary>
    /// Performance improvements achieved.
    /// </summary>
    public Dictionary<string, double> PerformanceImprovements { get; init; } = new();
    
    /// <summary>
    /// Strategy execution duration.
    /// </summary>
    public TimeSpan ExecutionDuration { get; init; }
    
    /// <summary>
    /// Strategy execution timestamp.
    /// </summary>
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
}