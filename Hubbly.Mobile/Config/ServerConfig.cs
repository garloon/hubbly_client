namespace Hubbly.Mobile.Config;

/// <summary>
/// Centralized server configuration.
/// The default URL can be overridden via Preferences with key "server_url".
/// </summary>
public static class ServerConfig
{
    /// <summary>
    /// Default server URL for development/testing.
    /// In production, this should be set via app settings or preferences.
    /// </summary>
    public const string DefaultServerUrl = "http://89.169.46.33:5000";

    /// <summary>
    /// Gets the server URL from Preferences, or falls back to DefaultServerUrl.
    /// </summary>
    public static string GetServerUrl()
    {
        return Preferences.Get("server_url", DefaultServerUrl);
    }
}
