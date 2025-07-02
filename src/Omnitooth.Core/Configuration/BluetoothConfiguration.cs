using System.ComponentModel.DataAnnotations;

namespace Omnitooth.Core.Configuration;

/// <summary>
/// Configuration for Bluetooth functionality.
/// </summary>
public class BluetoothConfiguration
{
    /// <summary>
    /// Gets or sets the device name that will be advertised.
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string DeviceName { get; set; } = "Omnitooth HID";

    /// <summary>
    /// Gets or sets the HID service UUID.
    /// </summary>
    [Required]
    public string ServiceUuid { get; set; } = "00001812-0000-1000-8000-00805f9b34fb";

    /// <summary>
    /// Gets or sets a value indicating whether to automatically reconnect to devices.
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    [Range(1000, 300000)]
    public int ConnectionTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the advertising interval in milliseconds.
    /// </summary>
    [Range(20, 10240)]
    public int AdvertisingIntervalMs { get; set; } = 100;
}