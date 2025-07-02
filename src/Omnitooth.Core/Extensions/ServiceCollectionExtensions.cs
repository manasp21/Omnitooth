using Microsoft.Extensions.DependencyInjection;

namespace Omnitooth.Core.Extensions;

/// <summary>
/// Extension methods for service collection configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Register core domain services
        // Currently minimal as this is the domain layer
        
        return services;
    }
}