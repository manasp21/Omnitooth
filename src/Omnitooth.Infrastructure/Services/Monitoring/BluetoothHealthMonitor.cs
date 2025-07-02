using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Omnitooth.Core.Configuration;
using Omnitooth.Core.Interfaces;
using Omnitooth.Core.Models;
using Omnitooth.Infrastructure.Services.Bluetooth;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Windows.Devices.Bluetooth;
using Windows.Devices.Radios;

namespace Omnitooth.Infrastructure.Services.Monitoring;

/// <summary>
/// Advanced health monitoring service for Bluetooth operations and system resources.
/// Provides real-time monitoring, metrics collection, and proactive issue detection.
/// </summary>
public class BluetoothHealthMonitor : IHealthMonitorService
{
    private readonly ILogger<BluetoothHealthMonitor> _logger;
    private readonly BluetoothConfiguration _config;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly Subject<HealthStatus> _healthStatusSubject = new();
    private readonly Subject<PerformanceMetrics> _metricsSubject = new();
    
    // Monitoring state
    private readonly ConcurrentQueue<OperationMetric> _operationMetrics = new();
    private readonly ConcurrentDictionary<string, CustomMetric> _customMetrics = new();
    private readonly ConcurrentDictionary<string, Func<CancellationToken, Task<ComponentHealthResult>>> _healthChecks = new();
    private readonly Timer _monitoringTimer;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;
    
    private HealthStatus _currentHealth = HealthStatus.Offline;
    private PerformanceMetrics _currentMetrics = new();
    private bool _isMonitoring;
    private bool _disposed;
    
    // Constants
    private const int MaxOperationMetrics = 10000;
    private const int MonitoringIntervalMs = 5000; // 5 seconds
    private const int MetricsRetentionHours = 24;

    /// <summary>
    /// Initializes a new instance of the <see cref="BluetoothHealthMonitor"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">Bluetooth configuration.</param>
    /// <param name="circuitBreaker">Circuit breaker for integration.</param>
    public BluetoothHealthMonitor(
        ILogger<BluetoothHealthMonitor> logger,
        IOptions<BluetoothConfiguration> options,
        ICircuitBreaker circuitBreaker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
        
        // Initialize performance counters
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize performance counters");
        }
        
        // Setup monitoring timer
        _monitoringTimer = new Timer(OnMonitoringTick, null, Timeout.Infinite, Timeout.Infinite);
        
        // Register default health checks
        RegisterDefaultHealthChecks();
        
        _logger.LogInformation("🔍 Bluetooth Health Monitor initialized");
    }

    /// <inheritdoc />
    public IObservable<HealthStatus> HealthStatusChanged => _healthStatusSubject.AsObservable();

    /// <inheritdoc />
    public IObservable<PerformanceMetrics> MetricsUpdated => _metricsSubject.AsObservable();

    /// <inheritdoc />
    public HealthStatus CurrentHealth => _currentHealth;

    /// <inheritdoc />
    public PerformanceMetrics CurrentMetrics => _currentMetrics;

    /// <inheritdoc />
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_isMonitoring)
        {
            _logger.LogWarning("Health monitoring is already running");
            return;
        }

        _logger.LogInformation("🚀 Starting Bluetooth health monitoring");
        
        try
        {
            // Perform initial health assessment
            await PerformInitialHealthAssessmentAsync(cancellationToken);
            
            // Start periodic monitoring
            _isMonitoring = true;
            _monitoringTimer.Change(MonitoringIntervalMs, MonitoringIntervalMs);
            
            _logger.LogInformation("✅ Bluetooth health monitoring started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to start health monitoring");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (!_isMonitoring)
        {
            return;
        }

        _logger.LogInformation("🛑 Stopping Bluetooth health monitoring");
        
        try
        {
            _isMonitoring = false;
            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Final health report
            var finalReport = await GetSystemHealthReportAsync(cancellationToken);
            _logger.LogInformation("📊 Final health report - Status: {Status}, Issues: {Issues}", 
                finalReport.OverallStatus, finalReport.CriticalIssues.Count);
            
            _currentHealth = HealthStatus.Offline;
            _healthStatusSubject.OnNext(_currentHealth);
            
            _logger.LogInformation("✅ Bluetooth health monitoring stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during health monitoring shutdown");
        }
        
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public void RecordSuccess(string operationType, TimeSpan duration)
    {
        var metric = new OperationMetric
        {
            OperationType = operationType,
            IsSuccess = true,
            Duration = duration,
            Timestamp = DateTime.UtcNow
        };
        
        EnqueueOperationMetric(metric);
        _logger.LogTrace("✅ Recorded successful operation: {OperationType} ({Duration}ms)", 
            operationType, duration.TotalMilliseconds);
    }

    /// <inheritdoc />
    public void RecordFailure(string operationType, TimeSpan duration, Exception error)
    {
        var metric = new OperationMetric
        {
            OperationType = operationType,
            IsSuccess = false,
            Duration = duration,
            Timestamp = DateTime.UtcNow,
            ErrorMessage = error.Message,
            Context = new Dictionary<string, object>
            {
                ["ExceptionType"] = error.GetType().Name,
                ["StackTrace"] = error.StackTrace ?? string.Empty
            }
        };
        
        EnqueueOperationMetric(metric);
        _logger.LogTrace("❌ Recorded failed operation: {OperationType} ({Duration}ms) - {Error}", 
            operationType, duration.TotalMilliseconds, error.Message);
    }

    /// <inheritdoc />
    public void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null)
    {
        var metric = new CustomMetric
        {
            Name = metricName,
            Value = value,
            Timestamp = DateTime.UtcNow,
            Tags = tags ?? new Dictionary<string, string>()
        };
        
        _customMetrics.AddOrUpdate(metricName, metric, (_, _) => metric);
        _logger.LogTrace("📊 Recorded custom metric: {MetricName} = {Value}", metricName, value);
    }

    /// <inheritdoc />
    public async Task<ComponentHealthResult> CheckComponentHealthAsync(string componentName, CancellationToken cancellationToken = default)
    {
        if (!_healthChecks.TryGetValue(componentName, out var healthCheck))
        {
            return new ComponentHealthResult
            {
                ComponentName = componentName,
                Status = HealthStatus.Critical,
                Description = $"No health check registered for component: {componentName}",
                CheckDuration = TimeSpan.Zero
            };
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await healthCheck(cancellationToken);
            stopwatch.Stop();
            
            return result with { CheckDuration = stopwatch.Elapsed };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "❌ Health check failed for component: {ComponentName}", componentName);
            
            return new ComponentHealthResult
            {
                ComponentName = componentName,
                Status = HealthStatus.Critical,
                Description = $"Health check failed: {ex.Message}",
                CheckDuration = stopwatch.Elapsed,
                Exception = ex,
                RecommendedActions = new List<string>
                {
                    "Check component configuration",
                    "Verify component dependencies",
                    "Review component logs for errors"
                }
            };
        }
    }

    /// <inheritdoc />
    public async Task<SystemHealthReport> GetSystemHealthReportAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var componentResults = new Dictionary<string, ComponentHealthResult>();
        var criticalIssues = new List<string>();
        var warnings = new List<string>();
        var recommendations = new List<string>();

        try
        {
            // Check all registered components
            var healthCheckTasks = _healthChecks.Keys.Select(async componentName =>
            {
                var result = await CheckComponentHealthAsync(componentName, cancellationToken);
                componentResults[componentName] = result;
                
                switch (result.Status)
                {
                    case HealthStatus.Critical:
                        criticalIssues.Add($"{componentName}: {result.Description}");
                        recommendations.AddRange(result.RecommendedActions);
                        break;
                    case HealthStatus.Degraded:
                        warnings.Add($"{componentName}: {result.Description}");
                        break;
                }
            });

            await Task.WhenAll(healthCheckTasks);
            
            // Determine overall status
            var overallStatus = HealthStatus.Healthy;
            if (criticalIssues.Any())
            {
                overallStatus = HealthStatus.Critical;
            }
            else if (warnings.Any())
            {
                overallStatus = HealthStatus.Degraded;
            }

            stopwatch.Stop();

            return new SystemHealthReport
            {
                OverallStatus = overallStatus,
                Metrics = _currentMetrics,
                ComponentResults = componentResults,
                CriticalIssues = criticalIssues,
                Warnings = warnings,
                Recommendations = recommendations,
                AssessmentDuration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "❌ Failed to generate system health report");
            
            return new SystemHealthReport
            {
                OverallStatus = HealthStatus.Critical,
                CriticalIssues = new List<string> { $"Health assessment failed: {ex.Message}" },
                AssessmentDuration = stopwatch.Elapsed
            };
        }
    }

    /// <inheritdoc />
    public void RegisterHealthCheck(string componentName, Func<CancellationToken, Task<ComponentHealthResult>> healthCheck)
    {
        _healthChecks.AddOrUpdate(componentName, healthCheck, (_, _) => healthCheck);
        _logger.LogDebug("🔧 Registered health check for component: {ComponentName}", componentName);
    }

    /// <inheritdoc />
    public void UnregisterHealthCheck(string componentName)
    {
        _healthChecks.TryRemove(componentName, out _);
        _logger.LogDebug("🔧 Unregistered health check for component: {ComponentName}", componentName);
    }

    /// <summary>
    /// Registers default health checks for core system components.
    /// </summary>
    private void RegisterDefaultHealthChecks()
    {
        // Bluetooth adapter health check
        RegisterHealthCheck("BluetoothAdapter", async ct =>
        {
            try
            {
                var adapter = await BluetoothAdapter.GetDefaultAsync();
                if (adapter == null)
                {
                    return new ComponentHealthResult
                    {
                        ComponentName = "BluetoothAdapter",
                        Status = HealthStatus.Critical,
                        Description = "No Bluetooth adapter found",
                        RecommendedActions = new List<string>
                        {
                            "Install Bluetooth adapter",
                            "Enable Bluetooth in device manager",
                            "Update Bluetooth drivers"
                        }
                    };
                }

                var radio = await adapter.GetRadioAsync();
                if (radio?.State != RadioState.On)
                {
                    return new ComponentHealthResult
                    {
                        ComponentName = "BluetoothAdapter",
                        Status = HealthStatus.Critical,
                        Description = $"Bluetooth radio is not enabled (State: {radio?.State})",
                        DiagnosticData = new Dictionary<string, object>
                        {
                            ["RadioState"] = radio?.State.ToString() ?? "Unknown",
                            ["AdapterSupportsLE"] = adapter.IsLowEnergySupported,
                            ["AdapterSupportsPeripheral"] = adapter.IsPeripheralRoleSupported
                        },
                        RecommendedActions = new List<string>
                        {
                            "Enable Bluetooth in Windows settings",
                            "Check Bluetooth adapter power state",
                            "Verify Bluetooth services are running"
                        }
                    };
                }

                return new ComponentHealthResult
                {
                    ComponentName = "BluetoothAdapter",
                    Status = HealthStatus.Healthy,
                    Description = "Bluetooth adapter is healthy and operational",
                    DiagnosticData = new Dictionary<string, object>
                    {
                        ["AdapterName"] = radio.Name,
                        ["AdapterSupportsLE"] = adapter.IsLowEnergySupported,
                        ["AdapterSupportsPeripheral"] = adapter.IsPeripheralRoleSupported,
                        ["RadioState"] = radio.State.ToString()
                    }
                };
            }
            catch (Exception ex)
            {
                return new ComponentHealthResult
                {
                    ComponentName = "BluetoothAdapter",
                    Status = HealthStatus.Critical,
                    Description = $"Bluetooth adapter check failed: {ex.Message}",
                    Exception = ex
                };
            }
        });

        // Circuit breaker health check
        RegisterHealthCheck("CircuitBreaker", async ct =>
        {
            try
            {
                var metrics = _circuitBreaker.GetMetrics();
                var status = metrics.State switch
                {
                    CircuitBreakerState.Closed => HealthStatus.Healthy,
                    CircuitBreakerState.HalfOpen => HealthStatus.Degraded,
                    CircuitBreakerState.Open => HealthStatus.Critical,
                    _ => HealthStatus.Critical
                };

                return new ComponentHealthResult
                {
                    ComponentName = "CircuitBreaker",
                    Status = status,
                    Description = $"Circuit breaker is in {metrics.State} state",
                    DiagnosticData = new Dictionary<string, object>
                    {
                        ["State"] = metrics.State.ToString(),
                        ["FailureCount"] = metrics.FailureCount,
                        ["SuccessCount"] = metrics.SuccessCount,
                        ["FailureRate"] = metrics.FailureRate,
                        ["RejectedCount"] = metrics.RejectedCount
                    }
                };
            }
            catch (Exception ex)
            {
                return new ComponentHealthResult
                {
                    ComponentName = "CircuitBreaker",
                    Status = HealthStatus.Critical,
                    Description = $"Circuit breaker check failed: {ex.Message}",
                    Exception = ex
                };
            }
        });

        // Memory health check
        RegisterHealthCheck("Memory", async ct =>
        {
            try
            {
                var memoryUsage = GC.GetTotalMemory(false);
                var memoryMB = memoryUsage / 1024 / 1024;
                
                var status = memoryMB switch
                {
                    < 100 => HealthStatus.Healthy,
                    < 500 => HealthStatus.Degraded,
                    _ => HealthStatus.Critical
                };

                return new ComponentHealthResult
                {
                    ComponentName = "Memory",
                    Status = status,
                    Description = $"Memory usage: {memoryMB} MB",
                    DiagnosticData = new Dictionary<string, object>
                    {
                        ["MemoryUsageMB"] = memoryMB,
                        ["Gen0Collections"] = GC.CollectionCount(0),
                        ["Gen1Collections"] = GC.CollectionCount(1),
                        ["Gen2Collections"] = GC.CollectionCount(2)
                    }
                };
            }
            catch (Exception ex)
            {
                return new ComponentHealthResult
                {
                    ComponentName = "Memory",
                    Status = HealthStatus.Critical,
                    Description = $"Memory check failed: {ex.Message}",
                    Exception = ex
                };
            }
        });
    }

    /// <summary>
    /// Performs initial health assessment when monitoring starts.
    /// </summary>
    private async Task PerformInitialHealthAssessmentAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔍 Performing initial health assessment");
        
        var report = await GetSystemHealthReportAsync(cancellationToken);
        _currentHealth = report.OverallStatus;
        
        _logger.LogInformation("📊 Initial health assessment complete - Status: {Status}", _currentHealth);
        _healthStatusSubject.OnNext(_currentHealth);
    }

    /// <summary>
    /// Handles periodic monitoring tick.
    /// </summary>
    private async void OnMonitoringTick(object? state)
    {
        if (!_isMonitoring || _disposed)
            return;

        try
        {
            await UpdateMetricsAsync();
            await CheckHealthStatusAsync();
            CleanupOldMetrics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during monitoring tick");
        }
    }

    /// <summary>
    /// Updates performance metrics.
    /// </summary>
    private async Task UpdateMetricsAsync()
    {
        try
        {
            var metrics = _operationMetrics.ToArray();
            var recentMetrics = metrics.Where(m => m.Timestamp > DateTime.UtcNow.AddMinutes(-5)).ToArray();
            
            var totalOps = recentMetrics.Length;
            var successfulOps = recentMetrics.Count(m => m.IsSuccess);
            var failedOps = totalOps - successfulOps;
            var successRate = totalOps > 0 ? (successfulOps * 100.0 / totalOps) : 0;
            
            var durations = recentMetrics.Select(m => m.Duration.TotalMilliseconds).ToArray();
            var avgLatency = durations.Any() ? durations.Average() : 0;
            var p95Latency = durations.Any() ? durations.OrderBy(d => d).Skip((int)(durations.Length * 0.95)).FirstOrDefault() : 0;
            
            var memoryUsage = GC.GetTotalMemory(false);
            var cpuUsage = GetCpuUsage();
            
            _currentMetrics = new PerformanceMetrics
            {
                TotalOperations = totalOps,
                SuccessfulOperations = successfulOps,
                FailedOperations = failedOps,
                SuccessRate = successRate,
                AverageLatencyMs = avgLatency,
                P95LatencyMs = p95Latency,
                OperationsPerSecond = totalOps / 5.0, // 5-minute window
                MemoryUsageBytes = memoryUsage,
                CpuUsagePercent = cpuUsage,
                BluetoothAdapterStatus = await GetBluetoothAdapterStatusAsync(),
                CircuitBreakerStatus = _circuitBreaker.State.ToString(),
                CustomMetrics = _customMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value)
            };
            
            _metricsSubject.OnNext(_currentMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to update metrics");
        }
    }

    /// <summary>
    /// Checks and updates health status.
    /// </summary>
    private async Task CheckHealthStatusAsync()
    {
        try
        {
            var report = await GetSystemHealthReportAsync();
            
            if (report.OverallStatus != _currentHealth)
            {
                var oldStatus = _currentHealth;
                _currentHealth = report.OverallStatus;
                
                _logger.LogInformation("🔄 Health status changed: {OldStatus} → {NewStatus}", 
                    oldStatus, _currentHealth);
                
                if (report.CriticalIssues.Any())
                {
                    _logger.LogWarning("⚠️ Critical issues detected: {Issues}", 
                        string.Join(", ", report.CriticalIssues));
                }
                
                _healthStatusSubject.OnNext(_currentHealth);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to check health status");
        }
    }

    /// <summary>
    /// Gets current CPU usage percentage.
    /// </summary>
    private double GetCpuUsage()
    {
        try
        {
            return _cpuCounter?.NextValue() ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets current Bluetooth adapter status.
    /// </summary>
    private async Task<string> GetBluetoothAdapterStatusAsync()
    {
        try
        {
            var adapter = await BluetoothAdapter.GetDefaultAsync();
            if (adapter == null)
                return "Not Available";
                
            var radio = await adapter.GetRadioAsync();
            return radio?.State.ToString() ?? "Unknown";
        }
        catch
        {
            return "Error";
        }
    }

    /// <summary>
    /// Enqueues an operation metric with size management.
    /// </summary>
    private void EnqueueOperationMetric(OperationMetric metric)
    {
        _operationMetrics.Enqueue(metric);
        
        // Maintain size limit
        while (_operationMetrics.Count > MaxOperationMetrics)
        {
            _operationMetrics.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Cleans up old metrics to prevent memory leaks.
    /// </summary>
    private void CleanupOldMetrics()
    {
        var cutoff = DateTime.UtcNow.AddHours(-MetricsRetentionHours);
        
        // Clean custom metrics
        var keysToRemove = _customMetrics
            .Where(kvp => kvp.Value.Timestamp < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();
            
        foreach (var key in keysToRemove)
        {
            _customMetrics.TryRemove(key, out _);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogDebug("🔧 Disposing Bluetooth Health Monitor");

        try
        {
            _ = StopMonitoringAsync();
            
            _monitoringTimer?.Dispose();
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
            _healthStatusSubject?.Dispose();
            _metricsSubject?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during health monitor disposal");
        }
        finally
        {
            _disposed = true;
            _logger.LogDebug("✅ Bluetooth Health Monitor disposed");
        }
    }
}