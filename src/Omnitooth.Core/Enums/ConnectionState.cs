namespace Omnitooth.Core.Enums;

/// <summary>
/// Represents the state of a Bluetooth connection.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Device is disconnected.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Device is connecting.
    /// </summary>
    Connecting,

    /// <summary>
    /// Device is connected and ready.
    /// </summary>
    Connected,

    /// <summary>
    /// Device is disconnecting.
    /// </summary>
    Disconnecting,

    /// <summary>
    /// Connection failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Connection is pairing.
    /// </summary>
    Pairing,

    /// <summary>
    /// Connection is paired.
    /// </summary>
    Paired
}