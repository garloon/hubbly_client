using Hubbly.Mobile.Services;
using Hubbly.Mobile.ViewModels;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Hubbly.Mobile.Views;

public partial class BootstrapperPage : ContentPage
{
    private readonly ILogger<BootstrapperPage> _logger;
    private readonly BootstrapperViewModel _viewModel;

    public BootstrapperPage(BootstrapperViewModel viewModel, ILogger<BootstrapperPage> logger)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;

        _logger = logger;

        NavigationPage.SetHasNavigationBar(this, false);

        _logger.LogInformation("BootstrapperPage created");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _logger.LogDebug("BootstrapperPage appearing");
        await _viewModel.StartAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        _logger.LogDebug("BootstrapperPage disappearing");
        await _viewModel.StopAsync();
    }
}
