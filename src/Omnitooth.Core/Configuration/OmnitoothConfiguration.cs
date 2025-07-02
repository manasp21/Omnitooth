using System.ComponentModel.DataAnnotations;

namespace Omnitooth.Core.Configuration;

/// <summary>
/// Root configuration for the Omnitooth application.
/// </summary>
public class OmnitoothConfiguration
{
    /// <summary>
    /// Gets or sets the Bluetooth configuration.
    /// </summary>
    [Required]
    public BluetoothConfiguration Bluetooth { get; set; } = new();

    /// <summary>
    /// Gets or sets the input capture configuration.
    /// </summary>
    [Required]
    public InputConfiguration Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the HID protocol configuration.
    /// </summary>
    [Required]
    public HidConfiguration HID { get; set; } = new();

    /// <summary>
    /// Gets or sets the security configuration.
    /// </summary>
    [Required]
    public SecurityConfiguration Security { get; set; } = new();

    /// <summary>
    /// Gets or sets the performance configuration.
    /// </summary>
    [Required]
    public PerformanceConfiguration Performance { get; set; } = new();

    /// <summary>
    /// Gets or sets the UI configuration.
    /// </summary>
    [Required]
    public UIConfiguration UI { get; set; } = new();
}