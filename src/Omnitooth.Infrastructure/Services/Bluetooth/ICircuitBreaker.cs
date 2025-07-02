using System;
using System.Threading;
using System.Threading.Tasks;

namespace Omnitooth.Infrastructure.Services.Bluetooth;

/// <summary>
/// Circuit breaker interface for managing service reliability and automatic recovery.
/// Implements the Circuit Breaker pattern to prevent cascading failures and enable graceful degradation.
/// </summary>
public interface ICircuitBreaker
{
    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    CircuitBreakerState State { get; }
    
    /// <summary>
    /// Gets the current failure count.
    /// </summary>
    int FailureCount { get; }
    
    /// <summary>
    /// Gets the timestamp when the circuit was last opened.
    /// </summary>
    DateTime? LastFailureTime { get; }
    
    /// <summary>
    /// Gets the next retry time when the circuit is open.
    /// </summary>
    DateTime? NextRetryTime { get; }
    
    /// <summary>
    /// Executes an operation with circuit breaker protection.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit is open and operation is rejected.</exception>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes an operation with circuit breaker protection.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit is open and operation is rejected.</exception>
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Manually opens the circuit breaker.
    /// </summary>
    void Open();
    
    /// <summary>
    /// Manually closes the circuit breaker and resets failure count.
    /// </summary>
    void Close();
    
    /// <summary>
    /// Forces the circuit breaker into half-open state for testing recovery.
    /// </summary>
    void HalfOpen();
    
    /// <summary>
    /// Resets the circuit breaker to closed state and clears all metrics.
    /// </summary>
    void Reset();
    
    /// <summary>
    /// Gets comprehensive metrics about the circuit breaker performance.
    /// </summary>
    CircuitBreakerMetrics GetMetrics();
}

/// <summary>
/// Represents the current state of a circuit breaker.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Circuit is closed - operations are allowed.
    /// </summary>
    Closed,
    
    /// <summary>
    /// Circuit is open - operations are rejected immediately.
    /// </summary>
    Open,
    
    /// <summary>
    /// Circuit is half-open - limited operations are allowed to test recovery.
    /// </summary>
    HalfOpen
}

/// <summary>
/// Exception thrown when circuit breaker is open and operation is rejected.
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State { get; }
    
    /// <summary>
    /// Gets the next retry time.
    /// </summary>
    public DateTime? NextRetryTime { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
    /// </summary>
    public CircuitBreakerOpenException(CircuitBreakerState state, DateTime? nextRetryTime)
        : base($"Circuit breaker is {state}. Operations are rejected.")
    {
        State = state;
        NextRetryTime = nextRetryTime;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
    /// </summary>
    public CircuitBreakerOpenException(CircuitBreakerState state, DateTime? nextRetryTime, string message)
        : base(message)
    {
        State = state;
        NextRetryTime = nextRetryTime;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
    /// </summary>
    public CircuitBreakerOpenException(CircuitBreakerState state, DateTime? nextRetryTime, string message, Exception innerException)
        : base(message, innerException)
    {
        State = state;
        NextRetryTime = nextRetryTime;
    }
}

/// <summary>
/// Comprehensive metrics for circuit breaker performance monitoring.
/// </summary>
public record CircuitBreakerMetrics
{
    /// <summary>
    /// Current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State { get; init; }
    
    /// <summary>
    /// Current failure count.
    /// </summary>
    public int FailureCount { get; init; }
    
    /// <summary>
    /// Total number of successful operations.
    /// </summary>
    public long SuccessCount { get; init; }
    
    /// <summary>
    /// Total number of failed operations.
    /// </summary>
    public long TotalFailureCount { get; init; }
    
    /// <summary>
    /// Total number of rejected operations (due to open circuit).
    /// </summary>
    public long RejectedCount { get; init; }
    
    /// <summary>
    /// Current failure rate as a percentage (0-100).
    /// </summary>
    public double FailureRate { get; init; }
    
    /// <summary>
    /// Timestamp when the circuit was last opened.
    /// </summary>
    public DateTime? LastFailureTime { get; init; }
    
    /// <summary>
    /// Next retry time when circuit is open.
    /// </summary>
    public DateTime? NextRetryTime { get; init; }
    
    /// <summary>
    /// Duration the circuit has been in current state.
    /// </summary>
    public TimeSpan TimeInCurrentState { get; init; }
    
    /// <summary>
    /// Average execution time for successful operations.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; init; }
    
    /// <summary>
    /// Timestamp when metrics were collected.
    /// </summary>
    public DateTime CollectedAt { get; init; } = DateTime.UtcNow;
}