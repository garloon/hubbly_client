using Android.Runtime;
using Hubbly.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Views;

[Preserve]
public partial class AvatarSelectionPage : ContentPage, IDisposable
{
    private readonly ILogger<AvatarSelectionPage> _logger;
    private readonly AvatarSelectionViewModel _viewModel;
    private bool _disposed;

    [Preserve]
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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _logger.LogDebug("AvatarSelectionPage appearing");
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

        _logger.LogDebug("AvatarSelectionPage disappearing");
        _viewModel.OnDisappearing();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("AvatarSelectionPage disposing...");

        // ������������ �� ������� ���� ����

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("AvatarSelectionPage disposed");
    }

    #endregion
}