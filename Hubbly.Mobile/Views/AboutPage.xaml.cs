using Hubbly.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Views;

public partial class AboutPage : ContentPage, IDisposable
{
    private readonly ILogger<AboutPage> _logger;
    private readonly AboutViewModel _viewModel;
    private bool _disposed;

    public AboutPage(AboutViewModel viewModel, ILogger<AboutPage> logger)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        BindingContext = _viewModel;

        _logger.LogDebug("AboutPage created");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogDebug("AboutPage appearing");

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
        _logger.LogDebug("AboutPage disappearing");
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogDebug("AboutPage disposing");

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
