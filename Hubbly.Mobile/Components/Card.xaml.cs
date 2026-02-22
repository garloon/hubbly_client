using Microsoft.Maui.Controls;

namespace Hubbly.Mobile.Components;

/// <summary>
/// Universal card component with adaptive styling
/// </summary>
public partial class Card : Border
{
    public Card()
    {
        InitializeComponent();
    }

    // Title property
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(Card),
            string.Empty,
            propertyChanged: OnContentChanged);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    // ShowTitle property
    public static readonly BindableProperty ShowTitleProperty =
        BindableProperty.Create(
            nameof(ShowTitle),
            typeof(bool),
            typeof(Card),
            true,
            propertyChanged: OnVisibilityChanged);

    public bool ShowTitle
    {
        get => (bool)GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    // Subtitle property
    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(
            nameof(Subtitle),
            typeof(string),
            typeof(Card),
            string.Empty,
            propertyChanged: OnContentChanged);

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    // ShowSubtitle property
    public static readonly BindableProperty ShowSubtitleProperty =
        BindableProperty.Create(
            nameof(ShowSubtitle),
            typeof(bool),
            typeof(Card),
            false,
            propertyChanged: OnVisibilityChanged);

    public bool ShowSubtitle
    {
        get => (bool)GetValue(ShowSubtitleProperty);
        set => SetValue(ShowSubtitleProperty, value);
    }

    // ShowSeparator property
    public static readonly BindableProperty ShowSeparatorProperty =
        BindableProperty.Create(
            nameof(ShowSeparator),
            typeof(bool),
            typeof(Card),
            false,
            propertyChanged: OnVisibilityChanged);

    public bool ShowSeparator
    {
        get => (bool)GetValue(ShowSeparatorProperty);
        set => SetValue(ShowSeparatorProperty, value);
    }

    // HasCustomContent property
    public static readonly BindableProperty HasCustomContentProperty =
        BindableProperty.Create(
            nameof(HasCustomContent),
            typeof(bool),
            typeof(Card),
            false,
            propertyChanged: OnVisibilityChanged);

    public bool HasCustomContent
    {
        get => (bool)GetValue(HasCustomContentProperty);
        set => SetValue(HasCustomContentProperty, value);
    }

    // ContentContainer property - exposes the internal StackLayout for custom content
    public StackLayout ContentContainer => ContentContainerInternal;

    // Adaptive font sizes
    public double TitleFontSize => 18;
    public double SubtitleFontSize => 14;

    private static void OnContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var card = (Card)bindable;
        card.UpdateContent();
    }

    private static void OnVisibilityChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var card = (Card)bindable;
        card.UpdateVisibility();
    }

    private void UpdateContent()
    {
        TitleLabel.Text = Title;
        SubtitleLabel.Text = Subtitle;
    }

    private void UpdateVisibility()
    {
        TitleLabel.IsVisible = ShowTitle && !string.IsNullOrEmpty(Title);
        SubtitleLabel.IsVisible = ShowSubtitle && !string.IsNullOrEmpty(Subtitle);
        Separator.IsVisible = ShowSeparator;
        ContentContainerInternal.IsVisible = HasCustomContent;
    }
}
