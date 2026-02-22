using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Hubbly.Mobile.Components;

/// <summary>
/// Universal page header with menu/back button and title
/// </summary>
public partial class PageHeader : ContentView
{
    public PageHeader()
    {
        InitializeComponent();
    }

    // Title property
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(PageHeader),
            string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    // ShowMenuButton property
    public static readonly BindableProperty ShowMenuButtonProperty =
        BindableProperty.Create(
            nameof(ShowMenuButton),
            typeof(bool),
            typeof(PageHeader),
            false);

    public bool ShowMenuButton
    {
        get => (bool)GetValue(ShowMenuButtonProperty);
        set => SetValue(ShowMenuButtonProperty, value);
    }

    // ToggleFlyoutCommand property
    public static readonly BindableProperty ToggleFlyoutCommandProperty =
        BindableProperty.Create(
            nameof(ToggleFlyoutCommand),
            typeof(ICommand),
            typeof(PageHeader),
            null);

    public ICommand ToggleFlyoutCommand
    {
        get => (ICommand)GetValue(ToggleFlyoutCommandProperty);
        set => SetValue(ToggleFlyoutCommandProperty, value);
    }

    // ShowActionButton property
    public static readonly BindableProperty ShowActionButtonProperty =
        BindableProperty.Create(
            nameof(ShowActionButton),
            typeof(bool),
            typeof(PageHeader),
            false);

    public bool ShowActionButton
    {
        get => (bool)GetValue(ShowActionButtonProperty);
        set => SetValue(ShowActionButtonProperty, value);
    }

    // ActionButtonText property
    public static readonly BindableProperty ActionButtonTextProperty =
        BindableProperty.Create(
            nameof(ActionButtonText),
            typeof(string),
            typeof(PageHeader),
            "←");

    public string ActionButtonText
    {
        get => (string)GetValue(ActionButtonTextProperty);
        set => SetValue(ActionButtonTextProperty, value);
    }

    // ActionCommand property
    public static readonly BindableProperty ActionCommandProperty =
        BindableProperty.Create(
            nameof(ActionCommand),
            typeof(ICommand),
            typeof(PageHeader),
            null);

    public ICommand ActionCommand
    {
        get => (ICommand)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    // Adaptive properties (computed based on screen size)
    public double MenuButtonSize => 44; // Base size, will be scaled by converter
    public double ActionButtonSize => 44;
    public double MenuButtonFontSize => 20;
    public double ActionButtonFontSize => 20;
    public double TitleFontSize => 20;
}
