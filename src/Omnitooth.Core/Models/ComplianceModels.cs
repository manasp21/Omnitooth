namespace Omnitooth.Core.Models;

/// <summary>
/// Represents the result of Bluetooth compliance validation.
/// </summary>
public record BluetoothComplianceResult
{
    /// <summary>
    /// Overall compliance status.
    /// </summary>
    public ComplianceStatus Status { get; init; }
    
    /// <summary>
    /// List of compliance issues found.
    /// </summary>
    public List<ComplianceIssue> Issues { get; init; } = new();
    
    /// <summary>
    /// List of warnings that don't prevent operation.
    /// </summary>
    public List<ComplianceWarning> Warnings { get; init; } = new();
    
    /// <summary>
    /// Detailed validation results by category.
    /// </summary>
    public Dictionary<string, ValidationResult> ValidationResults { get; init; } = new();
    
    /// <summary>
    /// Recommended actions to resolve issues.
    /// </summary>
    public List<string> RecommendedActions { get; init; } = new();
    
    /// <summary>
    /// Timestamp when validation was performed.
    /// </summary>
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Duration of the validation process.
    /// </summary>
    public TimeSpan ValidationDuration { get; init; }
}

/// <summary>
/// Represents overall compliance status.
/// </summary>
public enum ComplianceStatus
{
    /// <summary>
    /// Fully compliant - no issues found.
    /// </summary>
    Compliant,
    
    /// <summary>
    /// Compliant with warnings - operation possible but not optimal.
    /// </summary>
    CompliantWithWarnings,
    
    /// <summary>
    /// Non-compliant - critical issues that prevent operation.
    /// </summary>
    NonCompliant,
    
    /// <summary>
    /// Validation failed due to errors.
    /// </summary>
    ValidationFailed
}

/// <summary>
/// Represents a compliance issue that needs resolution.
/// </summary>
public record ComplianceIssue
{
    /// <summary>
    /// Severity level of the issue.
    /// </summary>
    public IssueSeverity Severity { get; init; }
    
    /// <summary>
    /// Category of the issue.
    /// </summary>
    public string Category { get; init; } = string.Empty;
    
    /// <summary>
    /// Code identifying the specific issue.
    /// </summary>
    public string IssueCode { get; init; } = string.Empty;
    
    /// <summary>
    /// Human-readable description of the issue.
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Detailed technical information about the issue.
    /// </summary>
    public string TechnicalDetails { get; init; } = string.Empty;
    
    /// <summary>
    /// Steps to resolve the issue.
    /// </summary>
    public List<string> ResolutionSteps { get; init; } = new();
    
    /// <summary>
    /// Whether the issue can be automatically remediated.
    /// </summary>
    public bool CanAutoRemediate { get; init; }
    
    /// <summary>
    /// Additional diagnostic data.
    /// </summary>
    public Dictionary<string, object> DiagnosticData { get; init; } = new();
}

/// <summary>
/// Represents the severity of a compliance issue.
/// </summary>
public enum IssueSeverity
{
    /// <summary>
    /// Low severity - minor optimization issue.
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium severity - may impact performance or reliability.
    /// </summary>
    Medium,
    
    /// <summary>
    /// High severity - likely to cause operational issues.
    /// </summary>
    High,
    
    /// <summary>
    /// Critical severity - prevents operation entirely.
    /// </summary>
    Critical
}

/// <summary>
/// Represents a compliance warning.
/// </summary>
public record ComplianceWarning
{
    /// <summary>
    /// Warning category.
    /// </summary>
    public string Category { get; init; } = string.Empty;
    
    /// <summary>
    /// Warning message.
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// Recommended action to address the warning.
    /// </summary>
    public string RecommendedAction { get; init; } = string.Empty;
    
    /// <summary>
    /// Additional context information.
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
}

/// <summary>
/// Represents a generic validation result.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Whether the validation passed.
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Additional validation details.
    /// </summary>
    public Dictionary<string, object> Details { get; init; } = new();
    
    /// <summary>
    /// Validation score (0-100).
    /// </summary>
    public double Score { get; init; }
}

/// <summary>
/// Represents Bluetooth adapter capability validation result.
/// </summary>
public record AdapterCapabilityResult : ValidationResult
{
    /// <summary>
    /// Whether Low Energy is supported.
    /// </summary>
    public bool SupportsLowEnergy { get; init; }
    
    /// <summary>
    /// Whether peripheral role is supported.
    /// </summary>
    public bool SupportsPeripheralRole { get; init; }
    
    /// <summary>
    /// Whether GATT server is supported.
    /// </summary>
    public bool SupportsGattServer { get; init; }
    
    /// <summary>
    /// Whether advertising is supported.
    /// </summary>
    public bool SupportsAdvertising { get; init; }
    
    /// <summary>
    /// Maximum number of concurrent connections.
    /// </summary>
    public int MaxConcurrentConnections { get; init; }
    
    /// <summary>
    /// Adapter hardware information.
    /// </summary>
    public string HardwareInfo { get; init; } = string.Empty;
    
    /// <summary>
    /// Driver version information.
    /// </summary>
    public string DriverVersion { get; init; } = string.Empty;
}

/// <summary>
/// Represents permission validation result.
/// </summary>
public record PermissionResult : ValidationResult
{
    /// <summary>
    /// Whether Bluetooth capability is declared.
    /// </summary>
    public bool HasBluetoothCapability { get; init; }
    
    /// <summary>
    /// Whether device capability is declared.
    /// </summary>
    public bool HasDeviceCapability { get; init; }
    
    /// <summary>
    /// Whether running with elevated privileges.
    /// </summary>
    public bool IsElevated { get; init; }
    
    /// <summary>
    /// Whether user consent is granted.
    /// </summary>
    public bool HasUserConsent { get; init; }
    
    /// <summary>
    /// List of missing permissions.
    /// </summary>
    public List<string> MissingPermissions { get; init; } = new();
}

/// <summary>
/// Represents application manifest validation result.
/// </summary>
public record ManifestValidationResult : ValidationResult
{
    /// <summary>
    /// Target Windows version from manifest.
    /// </summary>
    public string TargetVersion { get; init; } = string.Empty;
    
    /// <summary>
    /// Minimum Windows version from manifest.
    /// </summary>
    public string MinimumVersion { get; init; } = string.Empty;
    
    /// <summary>
    /// List of declared capabilities.
    /// </summary>
    public List<string> DeclaredCapabilities { get; init; } = new();
    
    /// <summary>
    /// List of missing required capabilities.
    /// </summary>
    public List<string> MissingCapabilities { get; init; } = new();
    
    /// <summary>
    /// Whether manifest is properly configured for Bluetooth.
    /// </summary>
    public bool IsBluetoothConfigured { get; init; }
}

/// <summary>
/// Represents Windows service dependency validation result.
/// </summary>
public record ServiceDependencyResult : ValidationResult
{
    /// <summary>
    /// Status of required Windows services.
    /// </summary>
    public Dictionary<string, ServiceStatus> ServiceStatuses { get; init; } = new();
    
    /// <summary>
    /// List of services that are not running.
    /// </summary>
    public List<string> NotRunningServices { get; init; } = new();
    
    /// <summary>
    /// Whether all required services are available.
    /// </summary>
    public bool AllServicesAvailable { get; init; }
}

/// <summary>
/// Represents the status of a Windows service.
/// </summary>
public enum ServiceStatus
{
    /// <summary>
    /// Service is running.
    /// </summary>
    Running,
    
    /// <summary>
    /// Service is stopped.
    /// </summary>
    Stopped,
    
    /// <summary>
    /// Service is starting.
    /// </summary>
    Starting,
    
    /// <summary>
    /// Service is stopping.
    /// </summary>
    Stopping,
    
    /// <summary>
    /// Service is disabled.
    /// </summary>
    Disabled,
    
    /// <summary>
    /// Service not found.
    /// </summary>
    NotFound
}

/// <summary>
/// Represents resource validation result.
/// </summary>
public record ResourceValidationResult : ValidationResult
{
    /// <summary>
    /// Available memory in bytes.
    /// </summary>
    public long AvailableMemoryBytes { get; init; }
    
    /// <summary>
    /// Current CPU usage percentage.
    /// </summary>
    public double CpuUsagePercent { get; init; }
    
    /// <summary>
    /// Whether system meets minimum resource requirements.
    /// </summary>
    public bool MeetsMinimumRequirements { get; init; }
    
    /// <summary>
    /// Whether system meets recommended resource requirements.
    /// </summary>
    public bool MeetsRecommendedRequirements { get; init; }
    
    /// <summary>
    /// Resource usage warnings.
    /// </summary>
    public List<string> ResourceWarnings { get; init; } = new();
}

/// <summary>
/// Represents the result of issue remediation.
/// </summary>
public record RemediationResult
{
    /// <summary>
    /// Whether remediation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// List of issues that were successfully remediated.
    /// </summary>
    public List<ComplianceIssue> RemediatedIssues { get; init; } = new();
    
    /// <summary>
    /// List of issues that could not be remediated.
    /// </summary>
    public List<ComplianceIssue> UnremediatedIssues { get; init; } = new();
    
    /// <summary>
    /// Details of remediation actions taken.
    /// </summary>
    public List<RemediationAction> ActionsTaken { get; init; } = new();
    
    /// <summary>
    /// Error messages from failed remediation attempts.
    /// </summary>
    public List<string> Errors { get; init; } = new();
    
    /// <summary>
    /// Whether system restart is required for changes to take effect.
    /// </summary>
    public bool RequiresRestart { get; init; }
}

/// <summary>
/// Represents a remediation action that was taken.
/// </summary>
public record RemediationAction
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
    /// Error message if action failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Timestamp when action was performed.
    /// </summary>
    public DateTime PerformedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a comprehensive system compatibility report.
/// </summary>
public record SystemCompatibilityReport
{
    /// <summary>
    /// Overall compatibility status.
    /// </summary>
    public ComplianceStatus OverallStatus { get; init; }
    
    /// <summary>
    /// Operating system information.
    /// </summary>
    public OperatingSystemInfo OSInfo { get; init; } = new();
    
    /// <summary>
    /// Hardware compatibility information.
    /// </summary>
    public HardwareCompatibilityInfo HardwareInfo { get; init; } = new();
    
    /// <summary>
    /// Software compatibility information.
    /// </summary>
    public SoftwareCompatibilityInfo SoftwareInfo { get; init; } = new();
    
    /// <summary>
    /// Compliance validation results.
    /// </summary>
    public BluetoothComplianceResult ComplianceResults { get; init; } = new();
    
    /// <summary>
    /// Timestamp when report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Compatibility score (0-100).
    /// </summary>
    public double CompatibilityScore { get; init; }
}

/// <summary>
/// Represents operating system information.
/// </summary>
public record OperatingSystemInfo
{
    /// <summary>
    /// Windows version string.
    /// </summary>
    public string Version { get; init; } = string.Empty;
    
    /// <summary>
    /// Windows build number.
    /// </summary>
    public string BuildNumber { get; init; } = string.Empty;
    
    /// <summary>
    /// Windows edition.
    /// </summary>
    public string Edition { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether OS is 64-bit.
    /// </summary>
    public bool Is64Bit { get; init; }
    
    /// <summary>
    /// Whether Windows Updates are available.
    /// </summary>
    public bool HasPendingUpdates { get; init; }
}

/// <summary>
/// Represents hardware compatibility information.
/// </summary>
public record HardwareCompatibilityInfo
{
    /// <summary>
    /// Bluetooth adapter information.
    /// </summary>
    public string AdapterInfo { get; init; } = string.Empty;
    
    /// <summary>
    /// Total system memory in bytes.
    /// </summary>
    public long TotalMemoryBytes { get; init; }
    
    /// <summary>
    /// Processor information.
    /// </summary>
    public string ProcessorInfo { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether hardware meets minimum requirements.
    /// </summary>
    public bool MeetsMinimumRequirements { get; init; }
}

/// <summary>
/// Represents software compatibility information.
/// </summary>
public record SoftwareCompatibilityInfo
{
    /// <summary>
    /// .NET runtime version.
    /// </summary>
    public string DotNetVersion { get; init; } = string.Empty;
    
    /// <summary>
    /// List of installed Windows features relevant to Bluetooth.
    /// </summary>
    public List<string> RelevantFeatures { get; init; } = new();
    
    /// <summary>
    /// List of conflicting software detected.
    /// </summary>
    public List<string> ConflictingSoftware { get; init; } = new();
    
    /// <summary>
    /// Whether all required software dependencies are met.
    /// </summary>
    public bool AllDependenciesMet { get; init; }
}