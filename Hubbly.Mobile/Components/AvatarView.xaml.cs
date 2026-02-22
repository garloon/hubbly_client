using Microsoft.Maui.Controls;

namespace Hubbly.Mobile.Components;

/// <summary>
/// Adaptive avatar component with online status indicator
/// </summary>
public partial class AvatarView : ContentView
{
    public AvatarView()
    {
        InitializeComponent();
    }

    // Nickname property (for initials generation)
    public static readonly BindableProperty NicknameProperty =
        BindableProperty.Create(
            nameof(Nickname),
            typeof(string),
            typeof(AvatarView),
            string.Empty,
            propertyChanged: OnAvatarPropertyChanged);

    public string Nickname
    {
        get => (string)GetValue(NicknameProperty);
        set => SetValue(NicknameProperty, value);
    }

    // AvatarColor property
    public static readonly BindableProperty AvatarColorProperty =
        BindableProperty.Create(
            nameof(AvatarColor),
            typeof(Color),
            typeof(AvatarView),
            Colors.Gray);

    public Color AvatarColor
    {
        get => (Color)GetValue(AvatarColorProperty);
        set => SetValue(AvatarColorProperty, value);
    }

    // Size enum for adaptive sizing
    public enum AvatarSize
    {
        Small,   // 32dp
        Medium,  // 40dp
        Large,   // 50dp
        XLarge   // 64dp
    }

    // Size property
    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(
            nameof(Size),
            typeof(AvatarSize),
            typeof(AvatarView),
            AvatarSize.Medium,
            propertyChanged: OnSizeChanged);

    public AvatarSize Size
    {
        get => (AvatarSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    // ShowOnlineStatus property
    public static readonly BindableProperty ShowOnlineStatusProperty =
        BindableProperty.Create(
            nameof(ShowOnlineStatus),
            typeof(bool),
            typeof(AvatarView),
            false);

    public bool ShowOnlineStatus
    {
        get => (bool)GetValue(ShowOnlineStatusProperty);
        set => SetValue(ShowOnlineStatusProperty, value);
    }

    // IsOnline property
    public static readonly BindableProperty IsOnlineProperty =
        BindableProperty.Create(
            nameof(IsOnline),
            typeof(bool),
            typeof(AvatarView),
            true);

    public bool IsOnline
    {
        get => (bool)GetValue(IsOnlineProperty);
        set => SetValue(IsOnlineProperty, value);
    }

    // AvatarImageSource property (for future use with real images)
    public static readonly BindableProperty AvatarImageSourceProperty =
        BindableProperty.Create(
            nameof(AvatarImageSource),
            typeof(ImageSource),
            typeof(AvatarView),
            null);

    public ImageSource AvatarImageSource
    {
        get => (ImageSource)GetValue(AvatarImageSourceProperty);
        set => SetValue(AvatarImageSourceProperty, value);
    }

    // Computed properties
    private bool ShowInitials => string.IsNullOrEmpty(AvatarImageSource?.ToString());
    private bool ShowImage => !string.IsNullOrEmpty(AvatarImageSource?.ToString());

    // CornerRadius based on size
    private double CornerRadius => Size switch
    {
        AvatarSize.Small => 16,
        AvatarSize.Medium => 20,
        AvatarSize.Large => 25,
        AvatarSize.XLarge => 32,
        _ => 20
    };

    // FontSize based on size
    private double FontSize => Size switch
    {
        AvatarSize.Small => 12,
        AvatarSize.Medium => 14,
        AvatarSize.Large => 18,
        AvatarSize.XLarge => 24,
        _ => 14
    };

    private static void OnAvatarPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AvatarView)bindable;
        control.UpdateDisplay();
    }

    private static void OnSizeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AvatarView)bindable;
        control.UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        // Update CornerRadius based on size
        AvatarRoundRectangle.CornerRadius = CornerRadius;
        
        // Update FontSize based on size
        InitialsLabel.FontSize = FontSize;

        // Update visibility of initials vs image
        InitialsLabel.IsVisible = ShowInitials;
        AvatarImage.IsVisible = ShowImage;

        // Update online indicator visibility
        OnlineIndicator.IsVisible = ShowOnlineStatus && IsOnline;
        OfflineIndicator.IsVisible = ShowOnlineStatus && !IsOnline;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(ShowOnlineStatus) ||
            propertyName == nameof(IsOnline))
        {
            UpdateDisplay();
        }
    }
}
