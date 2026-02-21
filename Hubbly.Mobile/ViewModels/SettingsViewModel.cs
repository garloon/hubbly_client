using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hubbly.Mobile.Services;
using Hubbly.Mobile.Converters;

namespace Hubbly.Mobile.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private bool _disposed;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private string _currentLanguage;

    [ObservableProperty]
    private bool _isRussian;

    [ObservableProperty]
    private bool _isEnglish;

    // Localized string properties
    [ObservableProperty]
    private string _settingsTitle = "Settings";

    [ObservableProperty]
    private string _themeLabel = "Theme";

    [ObservableProperty]
    private string _lightTheme = "Light";

    [ObservableProperty]
    private string _darkTheme = "Dark";

    [ObservableProperty]
    private string _languageLabel = "Language";

    [ObservableProperty]
    private string _russian = "Русский";

    [ObservableProperty]
    private string _english = "English";

    [ObservableProperty]
    private string _aboutAppName = "Hubbly Social Hub";

    [ObservableProperty]
    private string _aboutVersion = "Version";

    [ObservableProperty]
    private string _versionText = "Version 1.0";

    [ObservableProperty]
    private string _aboutDescription = "Real-time social hub with 3D avatars.";

    public ILocalizationService LocalizationService => _localizationService;

    public SettingsViewModel(IThemeService themeService, ILocalizationService localizationService)
    {
        _themeService = themeService;
        _localizationService = localizationService;

        // Initialize from services
        _isDarkTheme = _themeService.IsDarkTheme;
        _currentLanguage = _localizationService.CurrentLanguage;
        UpdateLanguageFlags();

        // Subscribe to service events
        _themeService.ThemeChanged += OnThemeChanged;
        _localizationService.LanguageChanged += OnLanguageChanged;

        // Set initial theme
        ApplyTheme();

        // Initialize localized strings
        SettingsTitle = _localizationService.GetString("settings_title");
        ThemeLabel = _localizationService.GetString("theme");
        LightTheme = _localizationService.GetString("light_theme");
        DarkTheme = _localizationService.GetString("dark_theme");
        LanguageLabel = _localizationService.GetString("language");
        Russian = _localizationService.GetString("russian");
        English = _localizationService.GetString("english");
        AboutAppName = _localizationService.GetString("about_app_name");
        AboutVersion = _localizationService.GetString("about_version");
        AboutDescription = _localizationService.GetString("about_description");
        
        UpdateVersionText();
    }

    private void OnThemeChanged(object? sender, bool isDark)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsDarkTheme = isDark;
            ApplyTheme();
        });
    }

    private void OnLanguageChanged(object? sender, string language)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentLanguage = language;
            UpdateLanguageFlags();
            
            // Update all localized strings
            SettingsTitle = _localizationService.GetString("settings_title");
            ThemeLabel = _localizationService.GetString("theme");
            LightTheme = _localizationService.GetString("light_theme");
            DarkTheme = _localizationService.GetString("dark_theme");
            LanguageLabel = _localizationService.GetString("language");
            Russian = _localizationService.GetString("russian");
            English = _localizationService.GetString("english");
            AboutAppName = _localizationService.GetString("about_app_name");
            AboutVersion = _localizationService.GetString("about_version");
            AboutDescription = _localizationService.GetString("about_description");
            
            UpdateVersionText();
            
            // Notify UI to update all bindings
            OnPropertyChanged(nameof(LocalizationService));
        });
    }

    private void UpdateLanguageFlags()
    {
        IsRussian = _currentLanguage == "ru";
        IsEnglish = _currentLanguage == "en";
    }

    private void ApplyTheme()
    {
        // Apply theme to application resources
        if (Application.Current != null)
        {
            var themeDict = _themeService.CurrentTheme;

            // Clear old theme resources
            var oldThemeKeys = Application.Current.Resources.Keys
                .Where(k => k.ToString().Contains("Color") || k.ToString().Contains("Background") || k.ToString().Contains("Text"))
                .ToList();

            foreach (var key in oldThemeKeys)
            {
                Application.Current.Resources.Remove(key);
            }

            // Add new theme resources
            foreach (var key in themeDict.Keys)
            {
                Application.Current.Resources[key] = themeDict[key];
            }
        }
    }

    private void UpdateVersionText()
    {
        VersionText = $"{AboutVersion} 1.0";
    }

    [RelayCommand]
    private void SetRussian()
    {
        if (_currentLanguage != "ru")
        {
            _localizationService.SetLanguage("ru");
        }
    }

    [RelayCommand]
    private void SetEnglish()
    {
        if (_currentLanguage != "en")
        {
            _localizationService.SetLanguage("en");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _themeService.ThemeChanged -= OnThemeChanged;
        _localizationService.LanguageChanged -= OnLanguageChanged;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
