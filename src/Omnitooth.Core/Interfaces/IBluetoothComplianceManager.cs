using Omnitooth.Core.Models;

namespace Omnitooth.Core.Interfaces;

/// <summary>
/// Interface for managing Windows Bluetooth API compliance and validation.
/// Ensures proper usage of Windows Runtime APIs and handles platform-specific requirements.
/// </summary>
public interface IBluetoothComplianceManager
{
    /// <summary>
    /// Validates Windows Bluetooth capabilities and requirements.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Compliance validation result.</returns>
    Task<BluetoothComplianceResult> ValidateComplianceAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if the current Windows version supports required Bluetooth features.
    /// </summary>
    /// <returns>True if Windows version is compatible.</returns>
    bool IsWindowsVersionSupported();
    
    /// <summary>
    /// Validates Bluetooth adapter capabilities for HID operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Adapter capability validation result.</returns>
    Task<AdapterCapabilityResult> ValidateAdapterCapabilitiesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks and requests necessary permissions for Bluetooth operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Permission validation result.</returns>
    Task<PermissionResult> ValidatePermissionsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates application manifest capabilities.
    /// </summary>
    /// <returns>Manifest validation result.</returns>
    ManifestValidationResult ValidateManifestCapabilities();
    
    /// <summary>
    /// Checks Windows service dependencies (Bluetooth Support Service, etc.).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Service dependency validation result.</returns>
    Task<ServiceDependencyResult> ValidateServiceDependenciesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates memory and resource limits for Bluetooth operations.
    /// </summary>
    /// <returns>Resource validation result.</returns>
    ResourceValidationResult ValidateResourceLimits();
    
    /// <summary>
    /// Performs automatic remediation for common compliance issues.
    /// </summary>
    /// <param name="issues">List of compliance issues to remediate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Remediation result.</returns>
    Task<RemediationResult> RemediateIssuesAsync(IEnumerable<ComplianceIssue> issues, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets detailed system compatibility report.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Comprehensive compatibility report.</returns>
    Task<SystemCompatibilityReport> GetCompatibilityReportAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers a custom compliance validator.
    /// </summary>
    /// <param name="validatorName">Name of the validator.</param>
    /// <param name="validator">Custom validation function.</param>
    void RegisterCustomValidator(string validatorName, Func<CancellationToken, Task<ValidationResult>> validator);
    
    /// <summary>
    /// Unregisters a custom compliance validator.
    /// </summary>
    /// <param name="validatorName">Name of the validator to remove.</param>
    void UnregisterCustomValidator(string validatorName);
}