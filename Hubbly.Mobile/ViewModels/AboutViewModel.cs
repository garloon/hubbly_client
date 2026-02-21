using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hubbly.Mobile.Services;

namespace Hubbly.Mobile.ViewModels;

public partial class AboutViewModel : ObservableObject, IDisposable
{
    private readonly ILocalizationService _localizationService;
    private bool _disposed;

    [ObservableProperty]
    private string _aboutTitle = "About";

    [ObservableProperty]
    private string _aboutAppName = "Hubbly Social Hub";

    [ObservableProperty]
    private string _aboutVersion = "Version";

    [ObservableProperty]
    private string _versionText = "Version 1.0";

    [ObservableProperty]
    private string _aboutDescription = "Real-time social hub with 3D avatars.";

    [ObservableProperty]
    private string _terms = "Terms of Service";

    [ObservableProperty]
    private string _privacy = "Privacy Policy";

    public ILocalizationService LocalizationService => _localizationService;

    public AboutViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService;

        // Subscribe to language changes
        _localizationService.LanguageChanged += OnLanguageChanged;

        // Initialize localized strings
        AboutTitle = _localizationService.GetString("about_title");
        AboutAppName = _localizationService.GetString("about_app_name");
        AboutVersion = _localizationService.GetString("about_version");
        AboutDescription = _localizationService.GetString("about_description");
        Terms = _localizationService.GetString("terms");
        Privacy = _localizationService.GetString("privacy");
        
        // Build version text
        UpdateVersionText();
    }

    private void OnLanguageChanged(object? sender, string language)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AboutTitle = _localizationService.GetString("about_title");
            AboutAppName = _localizationService.GetString("about_app_name");
            AboutVersion = _localizationService.GetString("about_version");
            AboutDescription = _localizationService.GetString("about_description");
            Terms = _localizationService.GetString("terms");
            Privacy = _localizationService.GetString("privacy");
            
            UpdateVersionText();
            
            OnPropertyChanged(nameof(LocalizationService));
        });
    }

    [RelayCommand]
    private async Task OpenTerms()
    {
        // TODO: Open Terms of Service URL
        await Application.Current.MainPage.DisplayAlert(
            "Terms of Service",
            "Terms of Service page will be available soon.",
            "OK");
    }

    [RelayCommand]
    private async Task OpenPrivacy()
    {
        // TODO: Open Privacy Policy URL
        await Application.Current.MainPage.DisplayAlert(
            "Privacy Policy",
            "Privacy Policy page will be available soon.",
            "OK");
    }

    private void UpdateVersionText()
    {
        VersionText = $"{AboutVersion} 1.0";
    }

    public void Dispose()
    {
        if (_disposed) return;

        _localizationService.LanguageChanged -= OnLanguageChanged;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
