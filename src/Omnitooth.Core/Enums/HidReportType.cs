namespace Omnitooth.Core.Enums;

/// <summary>
/// Represents the type of HID report.
/// </summary>
public enum HidReportType
{
    /// <summary>
    /// Input report (device to host).
    /// </summary>
    Input = 1,

    /// <summary>
    /// Output report (host to device).
    /// </summary>
    Output = 2,

    /// <summary>
    /// Feature report (bidirectional).
    /// </summary>
    Feature = 3
}