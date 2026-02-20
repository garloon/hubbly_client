using Hubbly.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Views;

public partial class AvatarSelectionPage : ContentPage, IDisposable
{
    private readonly ILogger<AvatarSelectionPage> _logger;
    private readonly AvatarSelectionViewModel _viewModel;
    private bool _disposed;

    public AvatarSelectionPage(AvatarSelectionViewModel viewModel, ILogger<AvatarSelectionPage> logger)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;

        _logger = logger;

        NavigationPage.SetHasNavigationBar(this, false);

        _logger.LogInformation("AvatarSelectionPage created");
    }

    #region Lifecycle

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _logger.LogDebug("AvatarSelectionPage appearing");
        _viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _logger.LogDebug("AvatarSelectionPage disappearing");
        _viewModel.OnDisappearing();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("AvatarSelectionPage disposing...");

        // Отписываемся от событий если есть

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("AvatarSelectionPage disposed");
    }

    #endregion
}