using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Hubbly.Mobile.Services;

public interface IThemeService
{
    bool IsDarkTheme { get; }
    event EventHandler<bool> ThemeChanged;
    void ToggleTheme();
    void SetTheme(bool isDark);
    ResourceDictionary CurrentTheme { get; }
}

public class ThemeService : IThemeService, IDisposable
{
    private readonly ILogger<ThemeService> _logger;
    private readonly string _themeKey = "app_theme";
    private bool _isDarkTheme;
    private bool _disposed;

    public event EventHandler<bool> ThemeChanged;

    public bool IsDarkTheme => _isDarkTheme;

    public ResourceDictionary CurrentTheme { get; private set; }

    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;

        // Load saved theme
        var savedTheme = Preferences.Get(_themeKey, "light");
        _isDarkTheme = savedTheme == "dark";

        _logger.LogInformation("ThemeService initialized. Current theme: {Theme}", _isDarkTheme ? "Dark" : "Light");

        ApplyTheme();
    }

    public void ToggleTheme()
    {
        SetTheme(!_isDarkTheme);
    }

    public void SetTheme(bool isDark)
    {
        if (_isDarkTheme == isDark)
            return;

        _isDarkTheme = isDark;
        Preferences.Set(_themeKey, _isDarkTheme ? "dark" : "light");

        ApplyTheme();
        ThemeChanged?.Invoke(this, _isDarkTheme);

        _logger.LogInformation("Theme changed to: {Theme}", _isDarkTheme ? "Dark" : "Light");
    }

    private void ApplyTheme()
    {
        // Create merged resource dictionary with theme-specific resources
        var themeDict = new ResourceDictionary();

        if (_isDarkTheme)
        {
            // Dark theme colors
            themeDict["PageBackgroundColor"] = Color.FromArgb("#1F2937"); // Gray-800
            themeDict["PrimaryColor"] = Color.FromArgb("#4F46E5"); // Indigo-600 (same purple)
            themeDict["TextColor"] = Color.FromArgb("#F8FAFC"); // Gray-50
            themeDict["SecondaryTextColor"] = Color.FromArgb("#9CA3AF"); // Gray-400
            themeDict["CardBackgroundColor"] = Color.FromArgb("#374151"); // Gray-700
            themeDict["BorderColor"] = Color.FromArgb("#4B5563"); // Gray-600
            themeDict["InputBackgroundColor"] = Color.FromArgb("#374151");
        }
        else
        {
            // Light theme colors
            themeDict["PageBackgroundColor"] = Color.FromArgb("#FFFFFF");
            themeDict["PrimaryColor"] = Color.FromArgb("#4F46E5"); // Same purple
            themeDict["TextColor"] = Color.FromArgb("#1E293B"); // Gray-800
            themeDict["SecondaryTextColor"] = Color.FromArgb("#64748B"); // Gray-500
            themeDict["CardBackgroundColor"] = Color.FromArgb("#FFFFFF");
            themeDict["BorderColor"] = Color.FromArgb("#E2E8F0"); // Gray-200
            themeDict["InputBackgroundColor"] = Color.FromArgb("#F1F5F9"); // Gray-100
        }

        CurrentTheme = themeDict;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("ThemeService disposing...");

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("ThemeService disposed");
    }
}
