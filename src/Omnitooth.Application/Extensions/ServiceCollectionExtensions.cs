using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Omnitooth.Application.Logging;
using Omnitooth.Application.Services;

namespace Omnitooth.Application.Extensions;

/// <summary>
/// Extension methods for service collection configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register logging helpers
        services.AddSingleton<PerformanceLogger>();

        // Register application services
        services.AddSingleton<IntegrationService>();
        services.AddHostedService<IntegrationService>(provider => provider.GetRequiredService<IntegrationService>());
        
        return services;
    }

    /// <summary>
    /// Configures logging for the application.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection ConfigureApplicationLogging(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information);
        });

        return services;
    }
}