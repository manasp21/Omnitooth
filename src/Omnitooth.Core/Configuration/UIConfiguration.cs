namespace Omnitooth.Core.Configuration;

/// <summary>
/// Configuration for user interface settings.
/// </summary>
public class UIConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether to start the application minimized.
    /// </summary>
    public bool StartMinimized { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to minimize to system tray.
    /// </summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show notifications.
    /// </summary>
    public bool ShowNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets the application theme.
    /// </summary>
    public string Theme { get; set; } = "System";

    /// <summary>
    /// Gets or sets a value indicating whether to auto-start with Windows.
    /// </summary>
    public bool AutoStart { get; set; } = false;
}