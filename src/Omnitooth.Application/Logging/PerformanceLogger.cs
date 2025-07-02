using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Omnitooth.Application.Logging;

/// <summary>
/// Helper class for performance logging.
/// </summary>
public class PerformanceLogger : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;
    private readonly object? _additionalProperties;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceLogger"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="additionalProperties">Additional properties to log.</param>
    public PerformanceLogger(ILogger logger, string operationName, object? additionalProperties = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        _additionalProperties = additionalProperties;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Stops the timer and logs the performance metrics.
    /// </summary>
    public void Dispose()
    {
        _stopwatch.Stop();
        _logger.LogPerformance(_operationName, _stopwatch.Elapsed, _additionalProperties);
    }

    /// <summary>
    /// Creates a new performance logger for the specified operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="additionalProperties">Additional properties to log.</param>
    /// <returns>A new performance logger.</returns>
    public static PerformanceLogger Start(ILogger logger, string operationName, object? additionalProperties = null)
    {
        return new PerformanceLogger(logger, operationName, additionalProperties);
    }
}

/// <summary>
/// Extension methods for creating performance loggers.
/// </summary>
public static class PerformanceLoggerExtensions
{
    /// <summary>
    /// Starts a performance logger for the specified operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="additionalProperties">Additional properties to log.</param>
    /// <returns>A new performance logger.</returns>
    public static PerformanceLogger StartPerformanceLogging(this ILogger logger, string operationName, object? additionalProperties = null)
    {
        return PerformanceLogger.Start(logger, operationName, additionalProperties);
    }
}