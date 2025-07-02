using Omnitooth.Core.Enums;

namespace Omnitooth.Core.Models;

/// <summary>
/// Represents keyboard input data.
/// </summary>
public class KeyboardInput
{
    /// <summary>
    /// Gets or sets the timestamp when the input was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the virtual key code.
    /// </summary>
    public uint VirtualKeyCode { get; set; }

    /// <summary>
    /// Gets or sets the scan code.
    /// </summary>
    public uint ScanCode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the key is pressed (true) or released (false).
    /// </summary>
    public bool IsPressed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is an extended key.
    /// </summary>
    public bool IsExtended { get; set; }

    /// <summary>
    /// Gets or sets the modifier keys state.
    /// </summary>
    public KeyboardModifiers Modifiers { get; set; }

    /// <summary>
    /// Gets or sets the input source type.
    /// </summary>
    public InputType InputType => InputType.Keyboard;
}

/// <summary>
/// Represents keyboard modifier key states.
/// </summary>
[Flags]
public enum KeyboardModifiers
{
    /// <summary>
    /// No modifiers.
    /// </summary>
    None = 0,

    /// <summary>
    /// Left Ctrl key.
    /// </summary>
    LeftCtrl = 1,

    /// <summary>
    /// Left Shift key.
    /// </summary>
    LeftShift = 2,

    /// <summary>
    /// Left Alt key.
    /// </summary>
    LeftAlt = 4,

    /// <summary>
    /// Left Windows key.
    /// </summary>
    LeftWin = 8,

    /// <summary>
    /// Right Ctrl key.
    /// </summary>
    RightCtrl = 16,

    /// <summary>
    /// Right Shift key.
    /// </summary>
    RightShift = 32,

    /// <summary>
    /// Right Alt key.
    /// </summary>
    RightAlt = 64,

    /// <summary>
    /// Right Windows key.
    /// </summary>
    RightWin = 128
}