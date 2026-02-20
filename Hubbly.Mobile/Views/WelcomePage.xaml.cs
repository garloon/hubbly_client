using Hubbly.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Views;

public partial class WelcomePage : ContentPage, IDisposable
{
    private readonly ILogger<WelcomePage> _logger;
    private readonly WelcomeViewModel _viewModel;
    private bool _disposed;

    public WelcomePage(WelcomeViewModel viewModel, ILogger<WelcomePage> logger)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;

        _logger = logger;

        NavigationPage.SetHasNavigationBar(this, false);

        _logger.LogInformation("WelcomePage created");
    }

    #region Lifecycle

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _logger.LogDebug("WelcomePage appearing");
        _viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _logger.LogDebug("WelcomePage disappearing");
        _viewModel.OnDisappearing();
    }

    #endregion

    #region Event Handlers

    private async void OnTermsTapped(object sender, EventArgs e)
    {
        try
        {
            _logger.LogDebug("Terms tapped");

            await DisplayAlert("Terms of Service",
                "Welcome to Hubbly! By using our service, you agree to be respectful to other users and follow community guidelines. We're building a positive social space together!",
                "Got it");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing terms");
        }
    }

    private async void OnPrivacyTapped(object sender, EventArgs e)
    {
        try
        {
            _logger.LogDebug("Privacy tapped");

            await DisplayAlert("Privacy Policy",
                "Your privacy matters. We only store your nickname and avatar preferences. Chat messages are ephemeral and not permanently stored. You can leave anytime!",
                "Understood");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing privacy policy");
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("WelcomePage disposing...");

        // Отписываемся от событий если есть

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("WelcomePage disposed");
    }

    #endregion
}