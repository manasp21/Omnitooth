using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Omnitooth.Core.Configuration;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Omnitooth.Infrastructure.Services.Bluetooth;

/// <summary>
/// Circuit breaker implementation specifically designed for Bluetooth operations.
/// Provides automatic failure detection, recovery, and resilience for Bluetooth services.
/// </summary>
public class BluetoothCircuitBreaker : ICircuitBreaker
{
    private readonly ILogger<BluetoothCircuitBreaker> _logger;
    private readonly CircuitBreakerOptions _options;
    private readonly object _stateLock = new();
    
    // State tracking
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount;
    private DateTime? _lastFailureTime;
    private DateTime? _stateChangedTime = DateTime.UtcNow;
    
    // Metrics tracking
    private long _successCount;
    private long _totalFailureCount;
    private long _rejectedCount;
    private readonly ConcurrentQueue<TimeSpan> _executionTimes = new();
    private const int MaxExecutionTimeSamples = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="BluetoothCircuitBreaker"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">Circuit breaker configuration options.</param>
    public BluetoothCircuitBreaker(ILogger<BluetoothCircuitBreaker> logger, IOptions<CircuitBreakerOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        _logger.LogInformation("🔌 Bluetooth Circuit Breaker initialized with settings:");
        _logger.LogInformation("   • Failure Threshold: {FailureThreshold}", _options.FailureThreshold);
        _logger.LogInformation("   • Recovery Timeout: {RecoveryTimeout}s", _options.RecoveryTimeout.TotalSeconds);
        _logger.LogInformation("   • Half-Open Max Attempts: {HalfOpenMaxAttempts}", _options.HalfOpenMaxAttempts);
    }

    /// <inheritdoc />
    public CircuitBreakerState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
    }

    /// <inheritdoc />
    public int FailureCount
    {
        get
        {
            lock (_stateLock)
            {
                return _failureCount;
            }
        }
    }

    /// <inheritdoc />
    public DateTime? LastFailureTime
    {
        get
        {
            lock (_stateLock)
            {
                return _lastFailureTime;
            }
        }
    }

    /// <inheritdoc />
    public DateTime? NextRetryTime
    {
        get
        {
            lock (_stateLock)
            {
                if (_state == CircuitBreakerState.Open && _lastFailureTime.HasValue)
                {
                    return _lastFailureTime.Value.Add(_options.RecoveryTimeout);
                }
                return null;
            }
        }
    }

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        // Check if operation should be allowed
        if (!ShouldAllowOperation())
        {
            Interlocked.Increment(ref _rejectedCount);
            var nextRetry = NextRetryTime;
            _logger.LogWarning("🚫 Circuit breaker rejected operation. State: {State}, Next retry: {NextRetry}", 
                State, nextRetry);
            throw new CircuitBreakerOpenException(State, nextRetry);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogDebug("⚡ Executing operation through circuit breaker (State: {State})", State);
            var result = await operation(cancellationToken);
            stopwatch.Stop();
            
            // Operation succeeded
            OnSuccess(stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Operation failed
            OnFailure(ex, stopwatch.Elapsed);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async ct =>
        {
            await operation(ct);
            return 0; // Dummy return value
        }, cancellationToken);
    }

    /// <inheritdoc />
    public void Open()
    {
        lock (_stateLock)
        {
            if (_state != CircuitBreakerState.Open)
            {
                _logger.LogWarning("🔴 Circuit breaker manually opened");
                ChangeState(CircuitBreakerState.Open);
            }
        }
    }

    /// <inheritdoc />
    public void Close()
    {
        lock (_stateLock)
        {
            if (_state != CircuitBreakerState.Closed)
            {
                _logger.LogInformation("🟢 Circuit breaker manually closed");
                ChangeState(CircuitBreakerState.Closed);
                _failureCount = 0;
            }
        }
    }

    /// <inheritdoc />
    public void HalfOpen()
    {
        lock (_stateLock)
        {
            if (_state != CircuitBreakerState.HalfOpen)
            {
                _logger.LogInformation("🟡 Circuit breaker manually set to half-open");
                ChangeState(CircuitBreakerState.HalfOpen);
            }
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_stateLock)
        {
            _logger.LogInformation("🔄 Circuit breaker reset");
            ChangeState(CircuitBreakerState.Closed);
            _failureCount = 0;
            _lastFailureTime = null;
            _successCount = 0;
            _totalFailureCount = 0;
            _rejectedCount = 0;
            
            // Clear execution time samples
            while (_executionTimes.TryDequeue(out _)) { }
        }
    }

    /// <inheritdoc />
    public CircuitBreakerMetrics GetMetrics()
    {
        lock (_stateLock)
        {
            var totalOperations = _successCount + _totalFailureCount;
            var failureRate = totalOperations > 0 ? (_totalFailureCount * 100.0 / totalOperations) : 0.0;
            
            var timeInState = _stateChangedTime.HasValue 
                ? DateTime.UtcNow - _stateChangedTime.Value 
                : TimeSpan.Zero;
            
            var avgExecutionTime = CalculateAverageExecutionTime();
            
            return new CircuitBreakerMetrics
            {
                State = _state,
                FailureCount = _failureCount,
                SuccessCount = _successCount,
                TotalFailureCount = _totalFailureCount,
                RejectedCount = _rejectedCount,
                FailureRate = failureRate,
                LastFailureTime = _lastFailureTime,
                NextRetryTime = NextRetryTime,
                TimeInCurrentState = timeInState,
                AverageExecutionTime = avgExecutionTime
            };
        }
    }

    /// <summary>
    /// Determines if an operation should be allowed based on current circuit state.
    /// </summary>
    private bool ShouldAllowOperation()
    {
        lock (_stateLock)
        {
            switch (_state)
            {
                case CircuitBreakerState.Closed:
                    return true;
                    
                case CircuitBreakerState.Open:
                    // Check if recovery timeout has elapsed
                    if (_lastFailureTime.HasValue && 
                        DateTime.UtcNow >= _lastFailureTime.Value.Add(_options.RecoveryTimeout))
                    {
                        _logger.LogInformation("🟡 Recovery timeout elapsed, transitioning to half-open");
                        ChangeState(CircuitBreakerState.HalfOpen);
                        return true;
                    }
                    return false;
                    
                case CircuitBreakerState.HalfOpen:
                    // Allow limited operations in half-open state
                    return _failureCount < _options.HalfOpenMaxAttempts;
                    
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Handles successful operation completion.
    /// </summary>
    private void OnSuccess(TimeSpan executionTime)
    {
        lock (_stateLock)
        {
            Interlocked.Increment(ref _successCount);
            RecordExecutionTime(executionTime);
            
            _logger.LogDebug("✅ Operation succeeded (Duration: {Duration}ms)", executionTime.TotalMilliseconds);
            
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _logger.LogInformation("🟢 Half-open test succeeded, closing circuit breaker");
                ChangeState(CircuitBreakerState.Closed);
                _failureCount = 0;
            }
            else if (_state == CircuitBreakerState.Closed && _failureCount > 0)
            {
                // Reset failure count on success in closed state
                _failureCount = 0;
                _logger.LogDebug("🔄 Failure count reset after successful operation");
            }
        }
    }

    /// <summary>
    /// Handles failed operation.
    /// </summary>
    private void OnFailure(Exception exception, TimeSpan executionTime)
    {
        lock (_stateLock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            Interlocked.Increment(ref _totalFailureCount);
            RecordExecutionTime(executionTime);
            
            _logger.LogWarning("❌ Operation failed (Duration: {Duration}ms, Failure #{FailureCount}): {Exception}", 
                executionTime.TotalMilliseconds, _failureCount, exception.Message);
            
            // Check if we should open the circuit
            if (_state == CircuitBreakerState.Closed && _failureCount >= _options.FailureThreshold)
            {
                _logger.LogError("🔴 Failure threshold ({Threshold}) reached, opening circuit breaker", 
                    _options.FailureThreshold);
                ChangeState(CircuitBreakerState.Open);
            }
            else if (_state == CircuitBreakerState.HalfOpen)
            {
                _logger.LogWarning("🔴 Half-open test failed, reopening circuit breaker");
                ChangeState(CircuitBreakerState.Open);
            }
        }
    }

    /// <summary>
    /// Changes the circuit breaker state and logs the transition.
    /// </summary>
    private void ChangeState(CircuitBreakerState newState)
    {
        var oldState = _state;
        _state = newState;
        _stateChangedTime = DateTime.UtcNow;
        
        _logger.LogInformation("🔄 Circuit breaker state changed: {OldState} → {NewState}", oldState, newState);
        
        if (newState == CircuitBreakerState.Open)
        {
            var nextRetry = _lastFailureTime?.Add(_options.RecoveryTimeout);
            _logger.LogWarning("⏰ Next retry attempt: {NextRetry}", nextRetry);
        }
    }

    /// <summary>
    /// Records execution time for performance metrics.
    /// </summary>
    private void RecordExecutionTime(TimeSpan executionTime)
    {
        _executionTimes.Enqueue(executionTime);
        
        // Keep only the last N samples
        while (_executionTimes.Count > MaxExecutionTimeSamples)
        {
            _executionTimes.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Calculates average execution time from recorded samples.
    /// </summary>
    private TimeSpan CalculateAverageExecutionTime()
    {
        var samples = _executionTimes.ToArray();
        if (samples.Length == 0)
            return TimeSpan.Zero;
            
        var totalTicks = samples.Sum(t => t.Ticks);
        return new TimeSpan(totalTicks / samples.Length);
    }
}

/// <summary>
/// Configuration options for the circuit breaker.
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Number of consecutive failures required to open the circuit.
    /// Default: 5
    /// </summary>
    public int FailureThreshold { get; set; } = 5;
    
    /// <summary>
    /// Time to wait before attempting recovery from open state.
    /// Default: 30 seconds
    /// </summary>
    public TimeSpan RecoveryTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Maximum number of test attempts allowed in half-open state.
    /// Default: 3
    /// </summary>
    public int HalfOpenMaxAttempts { get; set; } = 3;
    
    /// <summary>
    /// Timeout for individual operations.
    /// Default: 10 seconds
    /// </summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(10);
}