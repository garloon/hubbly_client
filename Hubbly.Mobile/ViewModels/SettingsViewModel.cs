using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hubbly.Mobile.Services;
using Hubbly.Mobile.Converters;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Hubbly.Mobile.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly AuthService _authService;
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

    // Guest conversion properties
    [ObservableProperty]
    private bool _isGuest = true; // TODO: определить из токена/сервиса

    [ObservableProperty]
    private string _convertGuestButtonText = "Стать пользователем";

    public ILocalizationService LocalizationService => _localizationService;

    public SettingsViewModel(IThemeService themeService, ILocalizationService localizationService, ILogger<SettingsViewModel> logger, AuthService authService)
    {
        _themeService = themeService;
        _localizationService = localizationService;
        _logger = logger;
        _authService = authService;

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

        // TODO: Определить реальный статус гостя из TokenManager
        // Пока заглушка для тестирования
        IsGuest = true;
        ConvertGuestButtonText = IsGuest ? "Стать пользователем" : "Вы уже пользователь";

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

    [RelayCommand]
    private void ToggleFlyout()
    {
        if (Shell.Current != null)
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        _logger.LogInformation("🔙 SettingsViewModel: GoBackCommand executed");
        
        try
        {
            // Try to close as modal first
            if (Shell.Current?.CurrentPage?.Navigation != null)
            {
                _logger.LogInformation("🔍 Attempting to pop modal page");
                await Shell.Current.CurrentPage.Navigation.PopModalAsync();
                _logger.LogInformation("✅ Successfully closed modal page");
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is NullReferenceException)
        {
            _logger.LogWarning(ex, "⚠️ Failed to close as modal (no modal pages), falling back to Shell navigation");
            // Fallback to Shell navigation
            await Shell.Current.GoToAsync("//chat");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error closing modal page");
            // Fallback to Shell navigation
            await Shell.Current.GoToAsync("//chat");
        }
    }

    [RelayCommand]
    private async Task ConvertGuest()
    {
        if (!IsGuest) return;

        _logger.LogInformation("🔄 Converting guest to user...");

        try
        {
            // Получить userId гостя из TokenManager
            var userIdStr = await _authService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var guestUserId))
            {
                await Application.Current?.MainPage?.DisplayAlert(
                    "Ошибка",
                    "Не удалось получить ID гостя",
                    "OK");
                return;
            }

            // Вызвать API конвертации
            var success = await _authService.ConvertGuestToUserAsync(guestUserId);

            if (success)
            {
                IsGuest = false;
                ConvertGuestButtonText = "Вы уже пользователь";

                _logger.LogInformation("✅ Guest converted to user successfully");

                await Application.Current?.MainPage?.DisplayAlert(
                    "Успех",
                    "Вы успешно стали пользователем!",
                    "OK");
            }
            else
            {
                await Application.Current?.MainPage?.DisplayAlert(
                    "Ошибка",
                    "Не удалось конвертировать гостя в пользователя",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Guest conversion failed");
            await Application.Current?.MainPage?.DisplayAlert(
                "Ошибка",
                "Не удалось конвертировать гостя в пользователя",
                "OK");
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
