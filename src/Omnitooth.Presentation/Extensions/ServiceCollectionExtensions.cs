using Microsoft.Extensions.DependencyInjection;
using Omnitooth.Presentation.Views;

namespace Omnitooth.Presentation.Extensions;

/// <summary>
/// Extension methods for service collection configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds presentation services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        // Register views
        services.AddTransient<MainWindow>();
        
        // Register view models
        // These will be implemented in later phases
        // Example: services.AddTransient<MainViewModel>();
        // Example: services.AddTransient<SettingsViewModel>();
        // Example: services.AddTransient<StatusViewModel>();
        
        return services;
    }
}