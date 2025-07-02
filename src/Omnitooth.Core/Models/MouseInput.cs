using Omnitooth.Core.Enums;

namespace Omnitooth.Core.Models;

/// <summary>
/// Represents mouse input data.
/// </summary>
public class MouseInput
{
    /// <summary>
    /// Gets or sets the timestamp when the input was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the X-axis movement delta.
    /// </summary>
    public int DeltaX { get; set; }

    /// <summary>
    /// Gets or sets the Y-axis movement delta.
    /// </summary>
    public int DeltaY { get; set; }

    /// <summary>
    /// Gets or sets the scroll wheel delta.
    /// </summary>
    public int ScrollDelta { get; set; }

    /// <summary>
    /// Gets or sets the horizontal scroll wheel delta.
    /// </summary>
    public int HorizontalScrollDelta { get; set; }

    /// <summary>
    /// Gets or sets the mouse button states.
    /// </summary>
    public MouseButtons ButtonStates { get; set; }

    /// <summary>
    /// Gets or sets the input source type.
    /// </summary>
    public InputType InputType => InputType.Mouse;
}

/// <summary>
/// Represents mouse button states.
/// </summary>
[Flags]
public enum MouseButtons
{
    /// <summary>
    /// No buttons pressed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Left mouse button.
    /// </summary>
    Left = 1,

    /// <summary>
    /// Right mouse button.
    /// </summary>
    Right = 2,

    /// <summary>
    /// Middle mouse button (wheel click).
    /// </summary>
    Middle = 4,

    /// <summary>
    /// Fourth mouse button (X1).
    /// </summary>
    X1 = 8,

    /// <summary>
    /// Fifth mouse button (X2).
    /// </summary>
    X2 = 16
}