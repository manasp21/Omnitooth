using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File;

namespace Omnitooth.Application.Logging;

/// <summary>
/// Extensions for logging configuration.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configures structured logging with context enrichment.
    /// </summary>
    /// <param name="configuration">The logger configuration.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <returns>The configured logger configuration.</returns>
    public static LoggerConfiguration ConfigureOmnitoothLogging(
        this LoggerConfiguration configuration,
        IHostEnvironment environment)
    {
        return configuration
            .MinimumLevel.Is(environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Omnitooth")
            .Enrich.WithProperty("Environment", environment.EnvironmentName)
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/omnitooth-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.Debug(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
    }

    /// <summary>
    /// Logs application startup information.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="environment">The hosting environment.</param>
    public static void LogApplicationStartup(this Microsoft.Extensions.Logging.ILogger logger, IHostEnvironment environment)
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        var framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        var osDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

        logger.LogInformation("Starting Omnitooth v{Version} in {Environment} mode", version, environment.EnvironmentName);
        logger.LogInformation("Framework: {Framework}", framework);
        logger.LogInformation("Operating System: {OperatingSystem}", osDescription);
        logger.LogInformation("Process ID: {ProcessId}", Environment.ProcessId);
        logger.LogInformation("Machine Name: {MachineName}", Environment.MachineName);
    }

    /// <summary>
    /// Logs performance metrics.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="duration">The operation duration.</param>
    /// <param name="additionalProperties">Additional properties to log.</param>
    public static void LogPerformance(this Microsoft.Extensions.Logging.ILogger logger, string operationName, TimeSpan duration, object? additionalProperties = null)
    {
        if (additionalProperties != null)
        {
            logger.LogInformation("Performance: {OperationName} completed in {Duration}ms {@Properties}",
                operationName, duration.TotalMilliseconds, additionalProperties);
        }
        else
        {
            logger.LogInformation("Performance: {OperationName} completed in {Duration}ms",
                operationName, duration.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Logs a security event.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="eventType">The security event type.</param>
    /// <param name="details">Event details.</param>
    /// <param name="deviceId">Optional device ID.</param>
    public static void LogSecurityEvent(this Microsoft.Extensions.Logging.ILogger logger, string eventType, string details, string? deviceId = null)
    {
        if (!string.IsNullOrEmpty(deviceId))
        {
            logger.LogWarning("Security Event: {EventType} - {Details} (Device: {DeviceId})", eventType, details, deviceId);
        }
        else
        {
            logger.LogWarning("Security Event: {EventType} - {Details}", eventType, details);
        }
    }

    /// <summary>
    /// Logs a connection event.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="deviceName">The device name.</param>
    /// <param name="eventType">The connection event type.</param>
    /// <param name="details">Optional details.</param>
    public static void LogConnectionEvent(this Microsoft.Extensions.Logging.ILogger logger, string deviceId, string deviceName, string eventType, string? details = null)
    {
        if (!string.IsNullOrEmpty(details))
        {
            logger.LogInformation("Connection: {EventType} - Device {DeviceName} ({DeviceId}) - {Details}",
                eventType, deviceName, deviceId, details);
        }
        else
        {
            logger.LogInformation("Connection: {EventType} - Device {DeviceName} ({DeviceId})",
                eventType, deviceName, deviceId);
        }
    }
}