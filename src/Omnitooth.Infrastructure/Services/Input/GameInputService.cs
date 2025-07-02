using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Omnitooth.Core.Configuration;
using Omnitooth.Core.Enums;
using Omnitooth.Core.Interfaces;
using Omnitooth.Core.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;

namespace Omnitooth.Infrastructure.Services.Input;

/// <summary>
/// GameInput API-based input capture service for high-performance input handling.
/// </summary>
public sealed class GameInputService : IInputCaptureService
{
    private readonly ILogger<GameInputService> _logger;
    private readonly InputConfiguration _config;
    private readonly Subject<KeyboardInput> _keyboardSubject = new();
    private readonly Subject<MouseInput> _mouseSubject = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _isCapturing;
    private bool _isGameInputEnabled;
    private bool _disposed;

    // GameInput API P/Invoke declarations
    [DllImport("GameInput.dll", SetLastError = true)]
    private static extern int GameInputCreate(out IntPtr gameInput);

    [DllImport("GameInput.dll", SetLastError = true)]
    private static extern int GameInputGetCurrentReading(IntPtr gameInput, int deviceKind, IntPtr device, out IntPtr reading);

    [DllImport("GameInput.dll", SetLastError = true)]
    private static extern int GameInputGetDeviceInfo(IntPtr device, out GameInputDeviceInfo deviceInfo);

    [DllImport("GameInput.dll", SetLastError = true)]
    private static extern void GameInputStopReading(IntPtr reading);

    // GameInput constants
    private const int GAMEINPUT_KIND_KEYBOARD = 0x1;
    private const int GAMEINPUT_KIND_MOUSE = 0x2;

    [StructLayout(LayoutKind.Sequential)]
    private struct GameInputDeviceInfo
    {
        public int VendorId;
        public int ProductId;
        public int RevisionNumber;
        public int InterfaceNumber;
        public int CollectionNumber;
        public int Usage;
        public int UsagePage;
        public int DeviceFamily;
        public int DeviceKind;
        public IntPtr DeviceId;
        public IntPtr DisplayName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameInputService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="options">Input configuration options.</param>
    public GameInputService(ILogger<GameInputService> logger, IOptions<InputConfiguration> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = options?.Value ?? throw new ArgumentNullException(nameof(options));

        InitializeGameInput();
    }

    /// <inheritdoc />
    public IObservable<KeyboardInput> KeyboardInput => _keyboardSubject.AsObservable();

    /// <inheritdoc />
    public IObservable<MouseInput> MouseInput => _mouseSubject.AsObservable();

    /// <inheritdoc />
    public bool IsCapturing => _isCapturing;

    /// <inheritdoc />
    public bool IsGameInputEnabled => _isGameInputEnabled;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isCapturing)
        {
            _logger.LogWarning("Input capture is already running");
            return Task.CompletedTask;
        }

        if (!_isGameInputEnabled)
        {
            _logger.LogError("GameInput API is not available or not enabled");
            throw new InvalidOperationException("GameInput API is not available");
        }

        _logger.LogInformation("Starting GameInput-based input capture");

        try
        {
            _isCapturing = true;

            // Start input capture loop
            _ = Task.Run(async () => await InputCaptureLoopAsync(_cancellationTokenSource.Token), cancellationToken);

            _logger.LogInformation("GameInput capture started successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start GameInput capture");
            _isCapturing = false;
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isCapturing)
        {
            return;
        }

        _logger.LogInformation("Stopping GameInput capture");

        try
        {
            _isCapturing = false;
            _cancellationTokenSource.Cancel();

            // Allow some time for cleanup
            await Task.Delay(100, cancellationToken);

            _logger.LogInformation("GameInput capture stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping GameInput capture");
            throw;
        }
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
    /// Initializes the GameInput API.
    /// </summary>
    private void InitializeGameInput()
    {
        try
        {
            _logger.LogDebug("Initializing GameInput API");

            var result = GameInputCreate(out var gameInput);
            if (result == 0 && gameInput != IntPtr.Zero)
            {
                _isGameInputEnabled = true;
                _logger.LogInformation("GameInput API initialized successfully");
            }
            else
            {
                _logger.LogWarning("GameInput API initialization failed with result: {Result}", result);
                _isGameInputEnabled = false;
            }
        }
        catch (DllNotFoundException)
        {
            _logger.LogWarning("GameInput.dll not found. GameInput API will not be available");
            _isGameInputEnabled = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during GameInput API initialization");
            _isGameInputEnabled = false;
        }
    }

    /// <summary>
    /// Main input capture loop.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task InputCaptureLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting input capture loop");

        while (!cancellationToken.IsCancellationRequested && _isCapturing)
        {
            try
            {
                // Capture keyboard and mouse input
                await CaptureInputAsync(cancellationToken);

                // Small delay to prevent excessive CPU usage
                await Task.Delay(_config.InputPollingInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Input capture loop cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in input capture loop");
                
                // Brief delay before retrying
                await Task.Delay(1000, cancellationToken);
            }
        }

        _logger.LogDebug("Input capture loop ended");
    }

    /// <summary>
    /// Captures input from GameInput API.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task CaptureInputAsync(CancellationToken cancellationToken)
    {
        // Note: This is a simplified implementation
        // In a real implementation, you would:
        // 1. Enumerate GameInput devices
        // 2. Get current readings for keyboard and mouse
        // 3. Process the input data
        // 4. Convert to domain models
        // 5. Publish through reactive streams

        // For now, we'll log that the capture is running
        // Real implementation would call GameInput APIs here
        _logger.LogTrace("GameInput capture tick");
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogDebug("Disposing GameInputService");

        try
        {
            _cancellationTokenSource.Cancel();
            _isCapturing = false;

            _keyboardSubject?.Dispose();
            _mouseSubject?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during GameInputService disposal");
        }
        finally
        {
            _disposed = true;
            _logger.LogDebug("GameInputService disposed");
        }
    }
}