using System.ComponentModel.DataAnnotations;

namespace Omnitooth.Core.Configuration;

/// <summary>
/// Configuration for input capture functionality.
/// </summary>
public class InputConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable GameInput API.
    /// </summary>
    public bool EnableGameInput { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to fallback to Raw Input if GameInput fails.
    /// </summary>
    public bool FallbackToRawInput { get; set; } = true;

    /// <summary>
    /// Gets or sets the keyboard buffer size for simultaneous key presses.
    /// </summary>
    [Range(1, 14)]
    public int KeyboardBufferSize { get; set; } = 6;

    /// <summary>
    /// Gets or sets the mouse sensitivity multiplier.
    /// </summary>
    [Range(0.1, 10.0)]
    public double MouseSensitivity { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the dead zone threshold for mouse input.
    /// </summary>
    [Range(0.0, 1.0)]
    public double DeadZoneThreshold { get; set; } = 0.1;

    /// <summary>
    /// Gets or sets the input rate limit in milliseconds.
    /// </summary>
    [Range(0, 100)]
    public int InputRateLimitMs { get; set; } = 1;

    /// <summary>
    /// Gets or sets the input polling interval in milliseconds.
    /// </summary>
    [Range(1, 100)]
    public int InputPollingInterval { get; set; } = 8;
}