using Omnitooth.Core.Enums;
using Omnitooth.Core.Models;

namespace Omnitooth.Core.Events;

/// <summary>
/// Event raised when input is captured.
/// </summary>
public class InputCapturedEvent
{
    /// <summary>
    /// Gets or sets the input type.
    /// </summary>
    public InputType InputType { get; set; }

    /// <summary>
    /// Gets or sets the keyboard input (if applicable).
    /// </summary>
    public KeyboardInput? KeyboardInput { get; set; }

    /// <summary>
    /// Gets or sets the mouse input (if applicable).
    /// </summary>
    public MouseInput? MouseInput { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the input was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InputCapturedEvent"/> class for keyboard input.
    /// </summary>
    /// <param name="keyboardInput">The keyboard input.</param>
    public InputCapturedEvent(KeyboardInput keyboardInput)
    {
        InputType = InputType.Keyboard;
        KeyboardInput = keyboardInput ?? throw new ArgumentNullException(nameof(keyboardInput));
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InputCapturedEvent"/> class for mouse input.
    /// </summary>
    /// <param name="mouseInput">The mouse input.</param>
    public InputCapturedEvent(MouseInput mouseInput)
    {
        InputType = InputType.Mouse;
        MouseInput = mouseInput ?? throw new ArgumentNullException(nameof(mouseInput));
        Timestamp = DateTimeOffset.UtcNow;
    }
}