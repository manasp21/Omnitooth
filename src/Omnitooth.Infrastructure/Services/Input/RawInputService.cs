using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Omnitooth.Core.Configuration;
using Omnitooth.Core.Enums;
using Omnitooth.Core.Interfaces;
using Omnitooth.Core.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using static Omnitooth.Core.Models.MouseInput;

namespace Omnitooth.Infrastructure.Services.Input;

/// <summary>
/// Raw Input API-based fallback input capture service.
/// </summary>
public sealed class RawInputService : IInputCaptureService
{
    private readonly ILogger<RawInputService> _logger;
    private readonly InputConfiguration _config;
    private readonly Subject<KeyboardInput> _keyboardSubject = new();
    private readonly Subject<MouseInput> _mouseSubject = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _isCapturing;
    private bool _disposed;
    private IntPtr _windowHandle;

    // Raw Input API P/Invoke declarations
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    [DllImport("user32.dll")]
    private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    // Raw Input constants
    private const uint RID_INPUT = 0x10000003;
    private const uint RIM_TYPEKEYBOARD = 1;
    private const uint RIM_TYPEMOUSE = 0;
    private const ushort HID_USAGE_PAGE_GENERIC = 0x01;
    private const ushort HID_USAGE_GENERIC_KEYBOARD = 0x06;
    private const ushort HID_USAGE_GENERIC_MOUSE = 0x02;
    private const uint RIDEV_INPUTSINK = 0x00000100;

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWMOUSE
    {
        public ushort usFlags;
        public ushort usButtonFlags;
        public ushort usButtonData;
        public uint ulRawButtons;
        public int lLastX;
        public int lLastY;
        public uint ulExtraInformation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RawInputService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="options">Input configuration options.</param>
    public RawInputService(ILogger<RawInputService> logger, IOptions<InputConfiguration> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = options?.Value ?? throw new ArgumentNullException(nameof(options));

        InitializeRawInput();
    }

    /// <inheritdoc />
    public IObservable<KeyboardInput> KeyboardInput => _keyboardSubject.AsObservable();

    /// <inheritdoc />
    public IObservable<MouseInput> MouseInput => _mouseSubject.AsObservable();

    /// <inheritdoc />
    public bool IsCapturing => _isCapturing;

    /// <inheritdoc />
    public bool IsGameInputEnabled => false; // Raw Input is always a fallback

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isCapturing)
        {
            _logger.LogWarning("Raw Input capture is already running");
            return;
        }

        _logger.LogInformation("Starting Raw Input-based input capture");

        try
        {
            _isCapturing = true;

            // Register for raw input notifications
            RegisterRawInputDevicesInternal();

            _logger.LogInformation("Raw Input capture started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Raw Input capture");
            _isCapturing = false;
            throw;
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isCapturing)
        {
            return;
        }

        _logger.LogInformation("Stopping Raw Input capture");

        try
        {
            _isCapturing = false;
            _cancellationTokenSource.Cancel();

            // Unregister raw input devices
            UnregisterRawInputDevices();

            _logger.LogInformation("Raw Input capture stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping Raw Input capture");
            throw;
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public void SetCaptureEnabled(bool enabled)
    {
        if (enabled && !_isCapturing)
        {
            _ = StartAsync();
        }
        else if (!enabled && _isCapturing)
        {
            _ = StopAsync();
        }
    }

    /// <summary>
    /// Initializes the Raw Input system.
    /// </summary>
    private void InitializeRawInput()
    {
        try
        {
            _logger.LogDebug("Initializing Raw Input API");

            // Create a hidden window to receive raw input messages
            var hInstance = GetModuleHandle("kernel32.dll");
            _windowHandle = CreateWindowEx(
                0,
                "STATIC",
                "OmnitoothRawInputWindow",
                0,
                0, 0, 0, 0,
                IntPtr.Zero,
                IntPtr.Zero,
                hInstance,
                IntPtr.Zero);

            if (_windowHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create raw input window");
            }

            _logger.LogInformation("Raw Input API initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Raw Input API");
            throw;
        }
    }

    /// <summary>
    /// Registers raw input devices.
    /// </summary>
    private void RegisterRawInputDevicesInternal()
    {
        var devices = new RAWINPUTDEVICE[]
        {
            new()
            {
                usUsagePage = HID_USAGE_PAGE_GENERIC,
                usUsage = HID_USAGE_GENERIC_KEYBOARD,
                dwFlags = RIDEV_INPUTSINK,
                hwndTarget = _windowHandle
            },
            new()
            {
                usUsagePage = HID_USAGE_PAGE_GENERIC,
                usUsage = HID_USAGE_GENERIC_MOUSE,
                dwFlags = RIDEV_INPUTSINK,
                hwndTarget = _windowHandle
            }
        };

        if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to register raw input devices. Error: {error}");
        }

        _logger.LogDebug("Raw input devices registered successfully");
    }

    /// <summary>
    /// Unregisters raw input devices.
    /// </summary>
    private void UnregisterRawInputDevices()
    {
        var devices = new RAWINPUTDEVICE[]
        {
            new()
            {
                usUsagePage = HID_USAGE_PAGE_GENERIC,
                usUsage = HID_USAGE_GENERIC_KEYBOARD,
                dwFlags = 0x00000001, // RIDEV_REMOVE
                hwndTarget = IntPtr.Zero
            },
            new()
            {
                usUsagePage = HID_USAGE_PAGE_GENERIC,
                usUsage = HID_USAGE_GENERIC_MOUSE,
                dwFlags = 0x00000001, // RIDEV_REMOVE
                hwndTarget = IntPtr.Zero
            }
        };

        RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
        _logger.LogDebug("Raw input devices unregistered");
    }

    /// <summary>
    /// Processes raw input data from keyboard.
    /// </summary>
    /// <param name="rawKeyboard">Raw keyboard data.</param>
    private void ProcessKeyboardInput(RAWKEYBOARD rawKeyboard)
    {
        try
        {
            var keyboardInput = new KeyboardInput
            {
                Timestamp = DateTimeOffset.UtcNow,
                VirtualKeyCode = rawKeyboard.VKey,
                ScanCode = rawKeyboard.MakeCode,
                IsPressed = (rawKeyboard.Flags & 0x01) == 0, // RI_KEY_BREAK
                IsExtended = (rawKeyboard.Flags & 0x02) != 0, // RI_KEY_E0
                Modifiers = GetCurrentKeyboardModifiers()
            };

            _keyboardSubject.OnNext(keyboardInput);
            _logger.LogTrace("Processed keyboard input: VKey={VKey}, IsPressed={IsPressed}", 
                rawKeyboard.VKey, keyboardInput.IsPressed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing keyboard input");
        }
    }

    /// <summary>
    /// Processes raw input data from mouse.
    /// </summary>
    /// <param name="rawMouse">Raw mouse data.</param>
    private void ProcessMouseInput(RAWMOUSE rawMouse)
    {
        try
        {
            var buttonStates = MouseButtons.None;
            if ((rawMouse.usButtonFlags & 0x0001) != 0) buttonStates |= MouseButtons.Left;
            if ((rawMouse.usButtonFlags & 0x0004) != 0) buttonStates |= MouseButtons.Right;
            if ((rawMouse.usButtonFlags & 0x0010) != 0) buttonStates |= MouseButtons.Middle;

            var mouseInput = new MouseInput
            {
                Timestamp = DateTimeOffset.UtcNow,
                DeltaX = rawMouse.lLastX,
                DeltaY = rawMouse.lLastY,
                ButtonStates = buttonStates,
                ScrollDelta = (short)(rawMouse.usButtonData >> 16)
            };

            _mouseSubject.OnNext(mouseInput);
            _logger.LogTrace("Processed mouse input: DeltaX={DeltaX}, DeltaY={DeltaY}", 
                rawMouse.lLastX, rawMouse.lLastY);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mouse input");
        }
    }

    /// <summary>
    /// Gets the current keyboard modifier state.
    /// </summary>
    /// <returns>Current keyboard modifiers.</returns>
    private KeyboardModifiers GetCurrentKeyboardModifiers()
    {
        // This would typically check the current state of modifier keys
        // For now, return None as a placeholder
        return KeyboardModifiers.None;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogDebug("Disposing RawInputService");

        try
        {
            _cancellationTokenSource.Cancel();
            _isCapturing = false;

            if (_windowHandle != IntPtr.Zero)
            {
                DestroyWindow(_windowHandle);
                _windowHandle = IntPtr.Zero;
            }

            _keyboardSubject?.Dispose();
            _mouseSubject?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during RawInputService disposal");
        }
        finally
        {
            _disposed = true;
            _logger.LogDebug("RawInputService disposed");
        }
    }
}