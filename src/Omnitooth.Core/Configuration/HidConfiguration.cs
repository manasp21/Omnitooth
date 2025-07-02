using System.ComponentModel.DataAnnotations;

namespace Omnitooth.Core.Configuration;

/// <summary>
/// Configuration for HID protocol functionality.
/// </summary>
public class HidConfiguration
{
    /// <summary>
    /// Gets or sets the HID report rate in Hz.
    /// </summary>
    [Range(1, 8000)]
    public int ReportRateHz { get; set; } = 1000;

    /// <summary>
    /// Gets or sets a value indicating whether to enable report batching.
    /// </summary>
    public bool EnableBatching { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum batch size for reports.
    /// </summary>
    [Range(1, 50)]
    public int BatchSizeLimit { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether to enable report compression.
    /// </summary>
    public bool CompressionEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the keyboard HID report ID.
    /// </summary>
    [Range(1, 255)]
    public byte KeyboardReportId { get; set; } = 1;

    /// <summary>
    /// Gets or sets the mouse HID report ID.
    /// </summary>
    [Range(1, 255)]
    public byte MouseReportId { get; set; } = 2;
}