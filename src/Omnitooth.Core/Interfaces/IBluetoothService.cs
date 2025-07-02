using Omnitooth.Core.Enums;
using Omnitooth.Core.Models;
using System.Reactive;

namespace Omnitooth.Core.Interfaces;

/// <summary>
/// Interface for Bluetooth GATT server service.
/// </summary>
public interface IBluetoothService : IDisposable
{
    /// <summary>
    /// Gets an observable stream of device connection events.
    /// </summary>
    IObservable<BluetoothDevice> DeviceConnected { get; }

    /// <summary>
    /// Gets an observable stream of device disconnection events.
    /// </summary>
    IObservable<BluetoothDevice> DeviceDisconnected { get; }

    /// <summary>
    /// Gets an observable stream of connection state change events.
    /// </summary>
    IObservable<(BluetoothDevice Device, ConnectionState OldState, ConnectionState NewState)> ConnectionStateChanged { get; }

    /// <summary>
    /// Gets the current service state.
    /// </summary>
    ServiceState ServiceState { get; }

    /// <summary>
    /// Gets the list of connected devices.
    /// </summary>
    IReadOnlyList<BluetoothDevice> ConnectedDevices { get; }

    /// <summary>
    /// Gets a value indicating whether the service is advertising.
    /// </summary>
    bool IsAdvertising { get; }

    /// <summary>
    /// Starts the Bluetooth GATT server and begins advertising.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the Bluetooth GATT server and stops advertising.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a HID report to all connected devices.
    /// </summary>
    /// <param name="report">The HID report to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendReportAsync(HidReport report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a HID report to a specific device.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="report">The HID report to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendReportToDeviceAsync(string deviceId, HidReport report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects a specific device.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectDeviceAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets device information by ID.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <returns>The device information, or null if not found.</returns>
    BluetoothDevice? GetDevice(string deviceId);
}