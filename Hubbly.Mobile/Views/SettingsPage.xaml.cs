using Android.Runtime;
using Hubbly.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Views;

[Preserve]
public partial class SettingsPage : ContentPage, IDisposable
{
    private readonly ILogger<SettingsPage> _logger;
    private readonly SettingsViewModel _viewModel;
    private bool _disposed;

    [Preserve]
    public SettingsPage(SettingsViewModel viewModel, ILogger<SettingsPage> logger)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        BindingContext = _viewModel;

        _logger.LogDebug("SettingsPage created");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogDebug("SettingsPage appearing");

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
        _logger.LogDebug("SettingsPage disappearing");
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogDebug("SettingsPage disposing");

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
