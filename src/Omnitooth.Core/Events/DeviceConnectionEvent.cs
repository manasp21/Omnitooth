using Omnitooth.Core.Enums;
using Omnitooth.Core.Models;

namespace Omnitooth.Core.Events;

/// <summary>
/// Event raised when a device connection state changes.
/// </summary>
public class DeviceConnectionEvent
{
    /// <summary>
    /// Gets or sets the device.
    /// </summary>
    public BluetoothDevice Device { get; set; }

    /// <summary>
    /// Gets or sets the previous connection state.
    /// </summary>
    public ConnectionState PreviousState { get; set; }

    /// <summary>
    /// Gets or sets the new connection state.
    /// </summary>
    public ConnectionState NewState { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the state changed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets an optional error message if the state change was due to an error.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets a value indicating whether the device just connected.
    /// </summary>
    public bool IsConnected => NewState == ConnectionState.Connected && PreviousState != ConnectionState.Connected;

    /// <summary>
    /// Gets a value indicating whether the device just disconnected.
    /// </summary>
    public bool IsDisconnected => NewState == ConnectionState.Disconnected && PreviousState != ConnectionState.Disconnected;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceConnectionEvent"/> class.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="previousState">The previous state.</param>
    /// <param name="newState">The new state.</param>
    /// <param name="errorMessage">Optional error message.</param>
    public DeviceConnectionEvent(BluetoothDevice device, ConnectionState previousState, ConnectionState newState, string? errorMessage = null)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        PreviousState = previousState;
        NewState = newState;
        Timestamp = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;
    }
}