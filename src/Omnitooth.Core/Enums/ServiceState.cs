namespace Omnitooth.Core.Enums;

/// <summary>
/// Represents the state of the Bluetooth HID service.
/// </summary>
public enum ServiceState
{
    /// <summary>
    /// Service is stopped.
    /// </summary>
    Stopped,

    /// <summary>
    /// Service is starting.
    /// </summary>
    Starting,

    /// <summary>
    /// Service is running and advertising.
    /// </summary>
    Running,

    /// <summary>
    /// Service is stopping.
    /// </summary>
    Stopping,

    /// <summary>
    /// Service encountered an error.
    /// </summary>
    Error
}