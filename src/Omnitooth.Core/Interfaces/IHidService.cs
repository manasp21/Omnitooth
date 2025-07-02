using Omnitooth.Core.Models;
using Omnitooth.Core.Enums;

namespace Omnitooth.Core.Interfaces;

/// <summary>
/// Interface for HID report service.
/// </summary>
public interface IHidService
{
    /// <summary>
    /// Creates a keyboard HID report from keyboard input.
    /// </summary>
    /// <param name="input">The keyboard input.</param>
    /// <returns>The HID report.</returns>
    HidReport CreateKeyboardReport(KeyboardInput input);

    /// <summary>
    /// Creates a mouse HID report from mouse input.
    /// </summary>
    /// <param name="input">The mouse input.</param>
    /// <returns>The HID report.</returns>
    HidReport CreateMouseReport(MouseInput input);

    /// <summary>
    /// Creates a combined HID report from keyboard and mouse input.
    /// </summary>
    /// <param name="keyboardInput">The keyboard input.</param>
    /// <param name="mouseInput">The mouse input.</param>
    /// <returns>The HID report.</returns>
    HidReport CreateCombinedReport(KeyboardInput? keyboardInput, MouseInput? mouseInput);

    /// <summary>
    /// Gets the HID report descriptor for the specified input type.
    /// </summary>
    /// <param name="inputType">The input type.</param>
    /// <returns>The HID report descriptor bytes.</returns>
    byte[] GetReportDescriptor(InputType inputType);

    /// <summary>
    /// Validates a HID report.
    /// </summary>
    /// <param name="report">The report to validate.</param>
    /// <returns>True if the report is valid; otherwise, false.</returns>
    bool ValidateReport(HidReport report);

    /// <summary>
    /// Gets the maximum report size for the specified input type.
    /// </summary>
    /// <param name="inputType">The input type.</param>
    /// <returns>The maximum report size in bytes.</returns>
    int GetMaxReportSize(InputType inputType);
}