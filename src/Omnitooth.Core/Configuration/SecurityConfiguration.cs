namespace Omnitooth.Core.Configuration;

/// <summary>
/// Configuration for security functionality.
/// </summary>
public class SecurityConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether to require device authentication.
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to require encrypted connections.
    /// </summary>
    public bool RequireEncryption { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of allowed device addresses.
    /// </summary>
    public List<string> AllowedDevices { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of blocked device addresses.
    /// </summary>
    public List<string> BlockedDevices { get; set; } = new();
}