using System.ComponentModel.DataAnnotations;
using System.Runtime;

namespace Omnitooth.Core.Configuration;

/// <summary>
/// Configuration for performance optimization.
/// </summary>
public class PerformanceConfiguration
{
    /// <summary>
    /// Gets or sets the number of worker threads in the thread pool.
    /// </summary>
    [Range(1, 100)]
    public int ThreadPoolWorkerThreads { get; set; } = 4;

    /// <summary>
    /// Gets or sets the number of completion port threads in the thread pool.
    /// </summary>
    [Range(1, 100)]
    public int ThreadPoolCompletionPortThreads { get; set; } = 4;

    /// <summary>
    /// Gets or sets the garbage collection latency mode.
    /// </summary>
    public string GCLatencyMode { get; set; } = "Interactive";

    /// <summary>
    /// Gets the parsed GC latency mode.
    /// </summary>
    public GCLatencyMode ParsedGCLatencyMode => Enum.TryParse<GCLatencyMode>(GCLatencyMode, out var mode) 
        ? mode 
        : System.Runtime.GCLatencyMode.Interactive;
}