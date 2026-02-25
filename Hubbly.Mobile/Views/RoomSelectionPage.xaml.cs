using Hubbly.Mobile.ViewModels;

namespace Hubbly.Mobile.Views;

public partial class RoomSelectionPage : ContentPage
{
    private readonly RoomSelectionViewModel _viewModel;

    public RoomSelectionPage(RoomSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
