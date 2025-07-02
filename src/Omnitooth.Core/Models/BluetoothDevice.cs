using Omnitooth.Core.Enums;

namespace Omnitooth.Core.Models;

/// <summary>
/// Represents a connected Bluetooth device.
/// </summary>
public class BluetoothDevice
{
    /// <summary>
    /// Gets or sets the device identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection state.
    /// </summary>
    public ConnectionState ConnectionState { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the device was first connected.
    /// </summary>
    public DateTimeOffset ConnectedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last activity.
    /// </summary>
    public DateTimeOffset LastActivity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the device is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the connection is encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Gets or sets the connection signal strength (RSSI).
    /// </summary>
    public int SignalStrength { get; set; }

    /// <summary>
    /// Gets or sets device-specific metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether the device is currently connected.
    /// </summary>
    public bool IsConnected => ConnectionState == ConnectionState.Connected;

    /// <summary>
    /// Initializes a new instance of the <see cref="BluetoothDevice"/> class.
    /// </summary>
    public BluetoothDevice()
    {
        ConnectedAt = DateTimeOffset.UtcNow;
        LastActivity = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the last activity timestamp.
    /// </summary>
    public void UpdateActivity()
    {
        LastActivity = DateTimeOffset.UtcNow;
    }
}