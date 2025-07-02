using Omnitooth.Core.Models;
using System.Reactive;

namespace Omnitooth.Core.Interfaces;

/// <summary>
/// Interface for input capture service.
/// </summary>
public interface IInputCaptureService : IDisposable
{
    /// <summary>
    /// Gets an observable stream of keyboard input events.
    /// </summary>
    IObservable<KeyboardInput> KeyboardInput { get; }

    /// <summary>
    /// Gets an observable stream of mouse input events.
    /// </summary>
    IObservable<MouseInput> MouseInput { get; }

    /// <summary>
    /// Gets a value indicating whether the service is currently capturing input.
    /// </summary>
    bool IsCapturing { get; }

    /// <summary>
    /// Gets a value indicating whether GameInput API is available and enabled.
    /// </summary>
    bool IsGameInputEnabled { get; }

    /// <summary>
    /// Starts capturing input from the specified devices.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops capturing input.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables input capture.
    /// </summary>
    /// <param name="enabled">Whether to enable input capture.</param>
    void SetCaptureEnabled(bool enabled);
}