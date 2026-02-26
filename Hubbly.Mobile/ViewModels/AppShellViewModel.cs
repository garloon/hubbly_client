using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hubbly.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.ViewModels;

public partial class AppShellViewModel : ObservableObject, IDisposable
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly TokenManager _tokenManager;
    private readonly INavigationService _navigationService;
    private readonly ILogger<AppShellViewModel> _logger;
    private bool _disposed;

    [ObservableProperty]
    private string _userInitials = "?";

    [ObservableProperty]
    private string _userNickname = "Guest";

    // Localized string properties
    [ObservableProperty]
    private string _chatRoomTitle = "Chat Room";

    [ObservableProperty]
    private string _settingsTitle = "Settings";

    [ObservableProperty]
    private string _aboutTitle = "About";

    [ObservableProperty]
    private string _logoutTitle = "Logout";

    public IThemeService ThemeService => _themeService;
    public ILocalizationService LocalizationService => _localizationService;

    public AppShellViewModel(IThemeService themeService,
                             ILocalizationService localizationService,
                             TokenManager tokenManager,
                             INavigationService navigationService,
                             ILogger<AppShellViewModel> logger)
    {
        _themeService = themeService;
        _localizationService = localizationService;
        _tokenManager = tokenManager;
        _navigationService = navigationService;
        _logger = logger;

        // Load user data
        _ = LoadUserDataAsync();

        // Subscribe to language changes
        _localizationService.LanguageChanged += OnLanguageChanged;

        // Initialize localized strings
        ChatRoomTitle = _localizationService.GetString("chat_room");
        SettingsTitle = _localizationService.GetString("settings_title");
        AboutTitle = _localizationService.GetString("about_title");
        LogoutTitle = _localizationService.GetString("logout_title");
    }

    private async Task LoadUserDataAsync()
    {
        try
        {
            var nickname = await _tokenManager.GetNicknameAsync();
            if (!string.IsNullOrEmpty(nickname))
            {
                UserNickname = nickname;
                UserInitials = GenerateInitials(nickname);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            System.Diagnostics.Debug.WriteLine($"Failed to load user data: {ex.Message}");
        }
    }

    private string GenerateInitials(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            return "?";

        var parts = nickname.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            var part = parts[0];
            return part.Length >= 2 ? part.Substring(0, 2).ToUpper() : part.Substring(0, 1).ToUpper();
        }

        var first = parts[0].FirstOrDefault();
        var last = parts[^1].FirstOrDefault();

        return $"{char.ToUpper(first)}{char.ToUpper(last)}";
    }

    private void OnLanguageChanged(object? sender, string language)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Update all localized string properties
            ChatRoomTitle = _localizationService.GetString("chat_room");
            SettingsTitle = _localizationService.GetString("settings_title");
            AboutTitle = _localizationService.GetString("about_title");
            LogoutTitle = _localizationService.GetString("logout_title");
            
            OnPropertyChanged(nameof(LocalizationService));
        });
    }

    [RelayCommand]
    private async Task Logout()
    {
        bool confirm = await Application.Current.MainPage.DisplayAlert(
            _localizationService.GetString("logout_title"),
            _localizationService.GetString("logout_confirm"),
            _localizationService.GetString("logout_yes"),
            _localizationService.GetString("logout_no"));

        if (confirm)
        {
            // Clear tokens
            _tokenManager.Clear();

            // Navigate back to Welcome page (clear navigation stack)
            await Shell.Current.GoToAsync("//welcome");
        }
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        _logger.LogInformation("🔰 AppShellViewModel: OpenSettingsCommand executed");
        await _navigationService.NavigateToAsync("//settings");
    }

    [RelayCommand]
    private async Task OpenAbout()
    {
        _logger.LogInformation("🔰 AppShellViewModel: OpenAboutCommand executed");
        await _navigationService.NavigateToAsync("//about");
    }

    [RelayCommand]
    private async Task OpenChat()
    {
        _logger.LogInformation("🔰 AppShellViewModel: OpenChatCommand executed");
        await _navigationService.NavigateToAsync("//chatroom");
    }

    [RelayCommand]
    private async Task OpenRoomSelection()
    {
        _logger.LogInformation("🔰 AppShellViewModel: OpenRoomSelectionCommand executed");
        
        // Get current room ID from TokenManager
        var currentRoomId = await _tokenManager.GetAsync("current_room_id");
        Guid? currentRoomGuid = null;
        if (!string.IsNullOrEmpty(currentRoomId) && Guid.TryParse(currentRoomId, out var guid))
        {
            currentRoomGuid = guid;
        }
        
        // Navigate modally with current room parameter
        var parameters = new Dictionary<string, object>
        {
            { "CurrentRoomId", currentRoomGuid }
        };
        
        await _navigationService.NavigateToAsync("//roomselection", parameters);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _localizationService.LanguageChanged -= OnLanguageChanged;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
