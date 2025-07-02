using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModernWpf;
using Omnitooth.Application.Logging;
using Omnitooth.Core.Configuration;
using Omnitooth.Presentation.Views;
using System.Windows;

namespace Omnitooth.Presentation;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private ILogger<App>? _logger;
    private OmnitoothConfiguration? _configuration;

    /// <summary>
    /// Gets or sets the application host.
    /// </summary>
    public static IHost? Host { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App()
    {
        // Dependencies will be resolved from Host when needed
    }

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    public IServiceProvider? Services => Host?.Services;

    /// <summary>
    /// Handles application startup.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The startup event args.</param>
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            // Initialize dependencies from Host
            if (Host?.Services == null)
                throw new InvalidOperationException("Host not initialized");

            _logger = Host.Services.GetRequiredService<ILogger<App>>();
            _configuration = Host.Services.GetRequiredService<IOptions<OmnitoothConfiguration>>().Value;

            // Log startup information
            _logger.LogApplicationStartup(Host.Services.GetRequiredService<IHostEnvironment>());

            // Apply theme
            ApplyTheme();

            // Start the host
            await Host.StartAsync();

            // Create and show main window
            var mainWindow = Host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;

            if (_configuration.UI.StartMinimized)
            {
                mainWindow.WindowState = WindowState.Minimized;
                if (_configuration.UI.MinimizeToTray)
                {
                    mainWindow.Hide();
                }
            }
            else
            {
                mainWindow.Show();
            }

            _logger.LogInformation("Omnitooth application started successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Failed to start Omnitooth application");
            MessageBox.Show(
                $"Failed to start application: {ex.Message}",
                "Omnitooth - Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <summary>
    /// Handles application exit.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The exit event args.</param>
    private async void Application_Exit(object sender, ExitEventArgs e)
    {
        try
        {
            _logger?.LogInformation("Shutting down Omnitooth application");
            if (Host != null)
            {
                await Host.StopAsync();
                Host.Dispose();
            }
            _logger?.LogInformation("Omnitooth application shut down successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during application shutdown");
        }
    }

    /// <summary>
    /// Applies the application theme.
    /// </summary>
    private void ApplyTheme()
    {
        ApplicationTheme? theme = _configuration?.UI.Theme.ToLowerInvariant() switch
        {
            "light" => ApplicationTheme.Light,
            "dark" => ApplicationTheme.Dark,
            _ => null // System theme
        };

        ThemeManager.Current.ApplicationTheme = theme;
    }
}