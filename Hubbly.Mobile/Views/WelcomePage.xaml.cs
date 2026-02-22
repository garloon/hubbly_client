using Android.Runtime;
using Hubbly.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Views;

[Preserve]
public partial class WelcomePage : ContentPage, IDisposable
{
    private readonly ILogger<WelcomePage> _logger;
    private readonly WelcomeViewModel _viewModel;
    private bool _disposed;

    [Preserve]
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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _logger.LogDebug("WelcomePage appearing");
        _viewModel.OnAppearing();

        // Fade-in animation
        if (MainGrid != null)
        {
            MainGrid.Opacity = 0;
            await MainGrid.FadeTo(1, 500, Easing.CubicInOut);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _logger.LogDebug("WelcomePage disappearing");
        _viewModel.OnDisappearing();
    }

    #endregion

    #region Event Handlers

    // Event handlers removed - using commands instead

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("WelcomePage disposing...");

        // ������������ �� ������� ���� ����

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("WelcomePage disposed");
    }

    #endregion
}