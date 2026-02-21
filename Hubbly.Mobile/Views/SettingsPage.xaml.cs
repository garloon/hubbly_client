using Hubbly.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Views;

public partial class SettingsPage : ContentPage, IDisposable
{
    private readonly ILogger<SettingsPage> _logger;
    private readonly SettingsViewModel _viewModel;
    private bool _disposed;

    public SettingsPage(SettingsViewModel viewModel, ILogger<SettingsPage> logger)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        BindingContext = _viewModel;

        _logger.LogDebug("SettingsPage created");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogDebug("SettingsPage appearing");
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
