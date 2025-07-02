using Omnitooth.Core.Enums;

namespace Omnitooth.Core.Events;

/// <summary>
/// Event raised when the service state changes.
/// </summary>
public class ServiceStateChangedEvent
{
    /// <summary>
    /// Gets or sets the previous service state.
    /// </summary>
    public ServiceState PreviousState { get; set; }

    /// <summary>
    /// Gets or sets the new service state.
    /// </summary>
    public ServiceState NewState { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the state changed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets an optional error message if the state change was due to an error.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceStateChangedEvent"/> class.
    /// </summary>
    /// <param name="previousState">The previous state.</param>
    /// <param name="newState">The new state.</param>
    /// <param name="errorMessage">Optional error message.</param>
    public ServiceStateChangedEvent(ServiceState previousState, ServiceState newState, string? errorMessage = null)
    {
        PreviousState = previousState;
        NewState = newState;
        Timestamp = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;
    }
}