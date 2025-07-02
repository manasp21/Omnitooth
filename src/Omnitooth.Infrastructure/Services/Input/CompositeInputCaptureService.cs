using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Omnitooth.Core.Configuration;
using Omnitooth.Core.Interfaces;
using Omnitooth.Core.Models;
using System.Reactive.Linq;

namespace Omnitooth.Infrastructure.Services.Input;

/// <summary>
/// Composite input capture service that automatically selects the best available input method.
/// </summary>
public sealed class CompositeInputCaptureService : IInputCaptureService
{
    private readonly ILogger<CompositeInputCaptureService> _logger;
    private readonly IInputCaptureService _primaryService;
    private readonly IInputCaptureService _fallbackService;
    private readonly IInputCaptureService _activeService;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeInputCaptureService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="options">Input configuration options.</param>
    public CompositeInputCaptureService(ILogger<CompositeInputCaptureService> logger, ILoggerFactory loggerFactory, IOptions<InputConfiguration> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var config = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // Create both services
        _primaryService = new GameInputService(
            loggerFactory.CreateLogger<GameInputService>(), 
            options);
        _fallbackService = new RawInputService(
            loggerFactory.CreateLogger<RawInputService>(), 
            options);

        // Choose the active service based on availability
        _activeService = SelectActiveService();

        _logger.LogInformation("Composite input capture service initialized with {ServiceType}", 
            _activeService.GetType().Name);
    }

    /// <inheritdoc />
    public IObservable<KeyboardInput> KeyboardInput => _activeService.KeyboardInput;

    /// <inheritdoc />
    public IObservable<MouseInput> MouseInput => _activeService.MouseInput;

    /// <inheritdoc />
    public bool IsCapturing => _activeService.IsCapturing;

    /// <inheritdoc />
    public bool IsGameInputEnabled => _activeService.IsGameInputEnabled;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting composite input capture using {ServiceType}", 
            _activeService.GetType().Name);
        
        await _activeService.StartAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping composite input capture");
        await _activeService.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void SetCaptureEnabled(bool enabled)
    {
        _activeService.SetCaptureEnabled(enabled);
    }

    /// <summary>
    /// Selects the active input service based on availability and configuration.
    /// </summary>
    /// <returns>The selected input service.</returns>
    private IInputCaptureService SelectActiveService()
    {
        // Prefer GameInput if available
        if (_primaryService.IsGameInputEnabled)
        {
            _logger.LogInformation("Selected GameInput API as primary input method");
            return _primaryService;
        }

        _logger.LogInformation("GameInput API not available, falling back to Raw Input API");
        return _fallbackService;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogDebug("Disposing CompositeInputCaptureService");

        try
        {
            _primaryService?.Dispose();
            _fallbackService?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during CompositeInputCaptureService disposal");
        }
        finally
        {
            _disposed = true;
            _logger.LogDebug("CompositeInputCaptureService disposed");
        }
    }
}