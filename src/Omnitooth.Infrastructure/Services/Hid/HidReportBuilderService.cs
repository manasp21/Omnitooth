using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Omnitooth.Core.Configuration;
using Omnitooth.Core.Enums;
using Omnitooth.Core.Interfaces;
using Omnitooth.Core.Models;
using static Omnitooth.Core.Models.MouseInput;

namespace Omnitooth.Infrastructure.Services.Hid;

/// <summary>
/// Service for building HID reports from input data.
/// </summary>
public sealed class HidReportBuilderService : IHidService
{
    private readonly ILogger<HidReportBuilderService> _logger;
    private readonly HidConfiguration _config;

    // HID Report Descriptor Constants
    private static readonly byte[] KeyboardReportDescriptor = new byte[]
    {
        0x05, 0x01,        // Usage Page (Generic Desktop Ctrls)
        0x09, 0x06,        // Usage (Keyboard)
        0xA1, 0x01,        // Collection (Application)
        0x05, 0x07,        //   Usage Page (Kbrd/Keypad)
        0x19, 0xE0,        //   Usage Minimum (0xE0)
        0x29, 0xE7,        //   Usage Maximum (0xE7)
        0x15, 0x00,        //   Logical Minimum (0)
        0x25, 0x01,        //   Logical Maximum (1)
        0x75, 0x01,        //   Report Size (1)
        0x95, 0x08,        //   Report Count (8)
        0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
        0x95, 0x01,        //   Report Count (1)
        0x75, 0x08,        //   Report Size (8)
        0x81, 0x01,        //   Input (Const,Array,Abs,No Wrap,Linear,Preferred State,No Null Position)
        0x95, 0x05,        //   Report Count (5)
        0x75, 0x01,        //   Report Size (1)
        0x05, 0x08,        //   Usage Page (LEDs)
        0x19, 0x01,        //   Usage Minimum (Num Lock)
        0x29, 0x05,        //   Usage Maximum (Kana)
        0x91, 0x02,        //   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
        0x95, 0x01,        //   Report Count (1)
        0x75, 0x03,        //   Report Size (3)
        0x91, 0x01,        //   Output (Const,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
        0x95, 0x06,        //   Report Count (6)
        0x75, 0x08,        //   Report Size (8)
        0x15, 0x00,        //   Logical Minimum (0)
        0x25, 0x65,        //   Logical Maximum (101)
        0x05, 0x07,        //   Usage Page (Kbrd/Keypad)
        0x19, 0x00,        //   Usage Minimum (0x00)
        0x29, 0x65,        //   Usage Maximum (0x65)
        0x81, 0x00,        //   Input (Data,Array,Abs,No Wrap,Linear,Preferred State,No Null Position)
        0xC0,              // End Collection
    };

    private static readonly byte[] MouseReportDescriptor = new byte[]
    {
        0x05, 0x01,        // Usage Page (Generic Desktop Ctrls)
        0x09, 0x02,        // Usage (Mouse)
        0xA1, 0x01,        // Collection (Application)
        0x09, 0x01,        //   Usage (Pointer)
        0xA1, 0x00,        //   Collection (Physical)
        0x05, 0x09,        //     Usage Page (Button)
        0x19, 0x01,        //     Usage Minimum (0x01)
        0x29, 0x05,        //     Usage Maximum (0x05)
        0x15, 0x00,        //     Logical Minimum (0)
        0x25, 0x01,        //     Logical Maximum (1)
        0x95, 0x05,        //     Report Count (5)
        0x75, 0x01,        //     Report Size (1)
        0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
        0x95, 0x01,        //     Report Count (1)
        0x75, 0x03,        //     Report Size (3)
        0x81, 0x01,        //     Input (Const,Array,Abs,No Wrap,Linear,Preferred State,No Null Position)
        0x05, 0x01,        //     Usage Page (Generic Desktop Ctrls)
        0x09, 0x30,        //     Usage (X)
        0x09, 0x31,        //     Usage (Y)
        0x09, 0x38,        //     Usage (Wheel)
        0x15, 0x81,        //     Logical Minimum (-127)
        0x25, 0x7F,        //     Logical Maximum (127)
        0x75, 0x08,        //     Report Size (8)
        0x95, 0x03,        //     Report Count (3)
        0x81, 0x06,        //     Input (Data,Var,Rel,No Wrap,Linear,Preferred State,No Null Position)
        0xC0,              //   End Collection
        0xC0,              // End Collection
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="HidReportBuilderService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="options">HID configuration options.</param>
    public HidReportBuilderService(ILogger<HidReportBuilderService> logger, IOptions<HidConfiguration> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public HidReport CreateKeyboardReport(KeyboardInput input)
    {
        _logger.LogTrace("Building keyboard HID report for VKey={VKey}, IsPressed={IsPressed}", 
            input.VirtualKeyCode, input.IsPressed);

        try
        {
            var reportData = new byte[8]; // Standard keyboard report size

            // Byte 0: Modifier keys
            reportData[0] = BuildModifierByte(input.Modifiers);

            // Byte 1: Reserved (always 0)
            reportData[1] = 0x00;

            // Bytes 2-7: Key array (up to 6 simultaneous keys)
            if (input.IsPressed)
            {
                var hidKeyCode = VirtualKeyToHidKeyCode(input.VirtualKeyCode);
                if (hidKeyCode != 0)
                {
                    reportData[2] = hidKeyCode;
                }
            }

            var report = new HidReport
            {
                ReportId = _config.KeyboardReportId,
                ReportType = HidReportType.Input,
                Data = reportData,
                Timestamp = input.Timestamp,
                InputType = InputType.Keyboard
            };

            _logger.LogTrace("Built keyboard HID report: ReportId={ReportId}, DataLength={DataLength}", 
                report.ReportId, report.Data.Length);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build keyboard HID report");
            throw;
        }
    }

    /// <inheritdoc />
    public HidReport CreateMouseReport(MouseInput input)
    {
        _logger.LogTrace("Building mouse HID report for DeltaX={DeltaX}, DeltaY={DeltaY}", 
            input.DeltaX, input.DeltaY);

        try
        {
            var reportData = new byte[4]; // Standard mouse report size

            // Byte 0: Button states
            reportData[0] = BuildMouseButtonByte(input);

            // Byte 1: X movement (signed 8-bit)
            reportData[1] = (byte)Math.Max(-127, Math.Min(127, input.DeltaX));

            // Byte 2: Y movement (signed 8-bit)
            reportData[2] = (byte)Math.Max(-127, Math.Min(127, input.DeltaY));

            // Byte 3: Wheel movement (signed 8-bit)
            reportData[3] = (byte)Math.Max(-127, Math.Min(127, input.ScrollDelta));

            var report = new HidReport
            {
                ReportId = _config.MouseReportId,
                ReportType = HidReportType.Input,
                Data = reportData,
                Timestamp = input.Timestamp,
                InputType = InputType.Mouse
            };

            _logger.LogTrace("Built mouse HID report: ReportId={ReportId}, DataLength={DataLength}", 
                report.ReportId, report.Data.Length);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build mouse HID report");
            throw;
        }
    }

    /// <inheritdoc />
    public HidReport CreateCombinedReport(KeyboardInput? keyboardInput, MouseInput? mouseInput)
    {
        _logger.LogTrace("Building combined HID report");

        try
        {
            // For now, prioritize keyboard input if both are provided
            if (keyboardInput != null)
            {
                return CreateKeyboardReport(keyboardInput);
            }
            
            if (mouseInput != null)
            {
                return CreateMouseReport(mouseInput);
            }

            // Return empty keyboard report if no input provided
            var emptyKeyboardInput = new KeyboardInput
            {
                Timestamp = DateTimeOffset.UtcNow,
                VirtualKeyCode = 0,
                ScanCode = 0,
                IsPressed = false,
                IsExtended = false,
                Modifiers = KeyboardModifiers.None
            };

            return CreateKeyboardReport(emptyKeyboardInput);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build combined HID report");
            throw;
        }
    }

    /// <inheritdoc />
    public byte[] GetReportDescriptor(InputType inputType)
    {
        _logger.LogDebug("Returning {InputType} HID report descriptor", inputType);
        
        return inputType switch
        {
            InputType.Keyboard => (byte[])KeyboardReportDescriptor.Clone(),
            InputType.Mouse => (byte[])MouseReportDescriptor.Clone(),
            _ => throw new ArgumentException($"Unsupported input type: {inputType}", nameof(inputType))
        };
    }

    /// <inheritdoc />
    public bool ValidateReport(HidReport report)
    {
        if (report == null)
        {
            return false;
        }

        try
        {
            // Validate report structure based on type
            bool isValid = report.InputType switch
            {
                InputType.Keyboard => ValidateKeyboardReport(report),
                InputType.Mouse => ValidateMouseReport(report),
                _ => false
            };

            _logger.LogTrace("HID report validation result: {IsValid} for {InputType}", 
                isValid, report.InputType);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating HID report");
            return false;
        }
    }

    /// <inheritdoc />
    public int GetMaxReportSize(InputType inputType)
    {
        return inputType switch
        {
            InputType.Keyboard => 8, // Standard keyboard report size
            InputType.Mouse => 4,    // Standard mouse report size
            _ => throw new ArgumentException($"Unsupported input type: {inputType}", nameof(inputType))
        };
    }

    /// <summary>
    /// Builds the modifier byte from keyboard modifiers.
    /// </summary>
    /// <param name="modifiers">Keyboard modifiers.</param>
    /// <returns>Modifier byte.</returns>
    private static byte BuildModifierByte(KeyboardModifiers modifiers)
    {
        byte result = 0;

        if (modifiers.HasFlag(KeyboardModifiers.LeftCtrl)) result |= 0x01;
        if (modifiers.HasFlag(KeyboardModifiers.LeftShift)) result |= 0x02;
        if (modifiers.HasFlag(KeyboardModifiers.LeftAlt)) result |= 0x04;
        if (modifiers.HasFlag(KeyboardModifiers.LeftWin)) result |= 0x08;
        if (modifiers.HasFlag(KeyboardModifiers.RightCtrl)) result |= 0x10;
        if (modifiers.HasFlag(KeyboardModifiers.RightShift)) result |= 0x20;
        if (modifiers.HasFlag(KeyboardModifiers.RightAlt)) result |= 0x40;
        if (modifiers.HasFlag(KeyboardModifiers.RightWin)) result |= 0x80;

        return result;
    }

    /// <summary>
    /// Builds the mouse button byte from mouse input.
    /// </summary>
    /// <param name="input">Mouse input.</param>
    /// <returns>Button byte.</returns>
    private static byte BuildMouseButtonByte(MouseInput input)
    {
        byte result = 0;

        if (input.ButtonStates.HasFlag(MouseButtons.Left)) result |= 0x01;
        if (input.ButtonStates.HasFlag(MouseButtons.Right)) result |= 0x02;
        if (input.ButtonStates.HasFlag(MouseButtons.Middle)) result |= 0x04;

        return result;
    }

    /// <summary>
    /// Converts a Windows virtual key code to HID usage ID.
    /// </summary>
    /// <param name="virtualKeyCode">Virtual key code.</param>
    /// <returns>HID usage ID.</returns>
    private static byte VirtualKeyToHidKeyCode(uint virtualKeyCode)
    {
        // Simplified mapping - in a real implementation, you'd have a comprehensive lookup table
        return virtualKeyCode switch
        {
            0x41 => 0x04, // A
            0x42 => 0x05, // B
            0x43 => 0x06, // C
            0x44 => 0x07, // D
            0x45 => 0x08, // E
            0x46 => 0x09, // F
            0x47 => 0x0A, // G
            0x48 => 0x0B, // H
            0x49 => 0x0C, // I
            0x4A => 0x0D, // J
            0x4B => 0x0E, // K
            0x4C => 0x0F, // L
            0x4D => 0x10, // M
            0x4E => 0x11, // N
            0x4F => 0x12, // O
            0x50 => 0x13, // P
            0x51 => 0x14, // Q
            0x52 => 0x15, // R
            0x53 => 0x16, // S
            0x54 => 0x17, // T
            0x55 => 0x18, // U
            0x56 => 0x19, // V
            0x57 => 0x1A, // W
            0x58 => 0x1B, // X
            0x59 => 0x1C, // Y
            0x5A => 0x1D, // Z
            0x31 => 0x1E, // 1
            0x32 => 0x1F, // 2
            0x33 => 0x20, // 3
            0x34 => 0x21, // 4
            0x35 => 0x22, // 5
            0x36 => 0x23, // 6
            0x37 => 0x24, // 7
            0x38 => 0x25, // 8
            0x39 => 0x26, // 9
            0x30 => 0x27, // 0
            0x0D => 0x28, // Enter
            0x1B => 0x29, // Escape
            0x08 => 0x2A, // Backspace
            0x09 => 0x2B, // Tab
            0x20 => 0x2C, // Space
            _ => 0x00     // Unknown/unmapped
        };
    }

    /// <summary>
    /// Validates a keyboard HID report.
    /// </summary>
    /// <param name="report">The HID report to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private bool ValidateKeyboardReport(HidReport report)
    {
        return report.Data != null &&
               report.Data.Length == 8 &&
               report.ReportId == _config.KeyboardReportId &&
               report.ReportType == HidReportType.Input;
    }

    /// <summary>
    /// Validates a mouse HID report.
    /// </summary>
    /// <param name="report">The HID report to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private bool ValidateMouseReport(HidReport report)
    {
        return report.Data != null &&
               report.Data.Length == 4 &&
               report.ReportId == _config.MouseReportId &&
               report.ReportType == HidReportType.Input;
    }
}