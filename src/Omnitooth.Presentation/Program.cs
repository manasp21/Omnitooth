using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Omnitooth.Application.Extensions;
using Omnitooth.Application.Logging;
using Omnitooth.Core.Configuration;
using Omnitooth.Core.Extensions;
using Omnitooth.Infrastructure.Extensions;
using Omnitooth.Presentation.Extensions;
using Serilog;
using System.IO;
using System.Threading;
using System.Windows;

namespace Omnitooth.Presentation;

/// <summary>
/// Main program entry point.
/// </summary>
public static class Program
{
    private static Mutex? _mutex;

    /// <summary>
    /// Application entry point.
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        // Ensure only one instance is running
        _mutex = new Mutex(true, "OmnitoothApplication", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "Omnitooth is already running.",
                "Omnitooth",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            // Build and run the application
            var host = CreateHostBuilder(args).Build();
            
            // Make host available to WPF Application
            App.Host = host;
            
            // Let WPF create and run the Application naturally
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fatal error: {ex.Message}",
                "Omnitooth - Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }

    /// <summary>
    /// Creates the host builder with configuration and services.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Configured host builder.</returns>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseContentRoot(GetContentRoot())
            .ConfigureAppConfiguration((context, config) =>
            {
                var env = context.HostingEnvironment;
                
                config.SetBasePath(env.ContentRootPath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables("OMNITOOTH_")
                    .AddCommandLine(args);
            })
            .UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .ConfigureOmnitoothLogging(context.HostingEnvironment)
                    .Enrich.WithProperty("Version", GetAssemblyVersion());
            })
            .ConfigureServices((context, services) =>
            {
                // Configure options
                services.Configure<OmnitoothConfiguration>(
                    context.Configuration.GetSection("Omnitooth"));

                // Configure application logging
                services.ConfigureApplicationLogging(context.HostingEnvironment);

                // Add application services
                services.AddCoreServices();
                services.AddInfrastructureServices(context.Configuration);
                services.AddApplicationServices();
                services.AddPresentationServices();
            });

    /// <summary>
    /// Gets the content root directory.
    /// </summary>
    /// <returns>Content root path.</returns>
    private static string GetContentRoot()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var location = assembly.Location;
        return Path.GetDirectoryName(location) ?? Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Gets the assembly version.
    /// </summary>
    /// <returns>Assembly version string.</returns>
    private static string GetAssemblyVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0.0";
    }
}