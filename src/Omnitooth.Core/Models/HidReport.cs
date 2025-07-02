using Omnitooth.Core.Enums;

namespace Omnitooth.Core.Models;

/// <summary>
/// Represents a HID report.
/// </summary>
public class HidReport
{
    /// <summary>
    /// Gets or sets the report ID.
    /// </summary>
    public byte ReportId { get; set; }

    /// <summary>
    /// Gets or sets the report type.
    /// </summary>
    public HidReportType ReportType { get; set; }

    /// <summary>
    /// Gets or sets the report data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the timestamp when the report was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the input type that generated this report.
    /// </summary>
    public InputType InputType { get; set; }

    /// <summary>
    /// Gets the size of the report data.
    /// </summary>
    public int Size => Data.Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="HidReport"/> class.
    /// </summary>
    public HidReport()
    {
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HidReport"/> class.
    /// </summary>
    /// <param name="reportId">The report ID.</param>
    /// <param name="reportType">The report type.</param>
    /// <param name="data">The report data.</param>
    /// <param name="inputType">The input type.</param>
    public HidReport(byte reportId, HidReportType reportType, byte[] data, InputType inputType)
    {
        ReportId = reportId;
        ReportType = reportType;
        Data = data ?? Array.Empty<byte>();
        InputType = inputType;
        Timestamp = DateTimeOffset.UtcNow;
    }
}