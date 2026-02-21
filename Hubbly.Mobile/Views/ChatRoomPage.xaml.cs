using Hubbly.Mobile.Services;
using Hubbly.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Views;

public partial class ChatRoomPage : ContentPage, IDisposable
{
    private readonly ILogger<ChatRoomPage> _logger;
    private readonly ChatRoomViewModel _viewModel;
    private readonly WebViewService _webViewService;
    private readonly AuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;
    private DateTime _lastTypingTime = DateTime.MinValue;

    public ChatRoomPage(
        ChatRoomViewModel viewModel,
        WebViewService webViewService,
        AuthService authService,
        INavigationService navigationService,
        ILogger<ChatRoomPage> logger)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _webViewService = webViewService ?? throw new ArgumentNullException(nameof(webViewService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        BindingContext = _viewModel;

        _logger = logger;

        NavigationPage.SetHasNavigationBar(this, false);

        // Initialize WebView (subscribe to events) before setting source
        InitializeWebView();

        // Set WebView source dynamically based on server URL from Preferences
        var serverUrl = Preferences.Get("server_url", "http://89.169.46.33:5000");
        AvatarWebView.Source = $"{serverUrl.TrimEnd('/')}/three_scene.html";
        _logger.LogInformation("WebView source set to: {Source}", AvatarWebView.Source);

        _logger.LogInformation("ChatRoomPage created");
    }

    #region Initialization

    private void InitializeWebView()
    {
        try
        {
            if (AvatarWebView == null)
            {
                _logger.LogError("AvatarWebView is null - 3D will be disabled");
                _viewModel.Disable3D();
                return;
            }

            // Log WebView source URL for debugging
            _logger.LogInformation("WebView source URL: {Source}", AvatarWebView.Source?.ToString() ?? "null");

            // Subscribe to WebView events
            AvatarWebView.Navigated += OnWebViewNavigated;
            AvatarWebView.Navigating += OnWebViewNavigating;

            // Initialize WebView service
            _webViewService.Initialize(AvatarWebView);
            _webViewService.OnSceneReady += OnSceneReady;
            _webViewService.OnSceneError += OnSceneError;

            _logger.LogInformation("WebView initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing WebView");
            _viewModel.Disable3D();
        }
    }

    private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
    {
        try
        {
            _logger.LogInformation("WebView navigating to: {Url}", e.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnWebViewNavigating");
        }
    }

    #endregion

    #region Lifecycle

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogDebug("ChatRoomPage appearing");

        try
        {
            // Re-initialize WebView source if it was reset in OnDisappearing
            if (AvatarWebView.Source == null)
            {
                _logger.LogInformation("Re-initializing WebView source after cache clear");
                var serverUrl = Preferences.Get("server_url", "http://89.169.46.33:5000");
                AvatarWebView.Source = $"{serverUrl.TrimEnd('/')}/three_scene.html";
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            
            await _viewModel.OnAppearing();

            // Check server availability
            var isAvailable = await _authService.CheckServerHealthAsync();

            if (!isAvailable)
            {
                _logger.LogWarning("Server unavailable");
                await ShowErrorAndGoBack("Chat server is not responding. Please try again later.");
                return;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Connection timeout");
            await ShowErrorAndGoBack("Connection timeout. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnAppearing");
            await ShowErrorAndGoBack("Failed to connect to chat server.");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _logger.LogDebug("ChatRoomPage disappearing");

        // IMPORTANT: Do NOT clear the 3D scene or disconnect from chat here.
        // When navigating to modal pages (Settings/About), ChatRoomPage remains visible
        // underneath, so we must preserve the scene state. Also, when returning from
        // modal pages, we want the scene to be intact.
        //
        // Cleanup (scene clearing, chat disconnection) is performed only in:
        // - DisposeAsync() when the page is permanently destroyed
        // - LogoutCommand when user explicitly logs out
        
        // We only cancel ongoing operations to prevent memory leaks
        _cts.Cancel();

        _logger.LogInformation("ChatRoomPage OnDisappearing: cancelled operations, scene preserved");
    }

    #endregion

    #region WebView Event Handlers

    private void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
    {
        try
        {
            _logger.LogInformation("WebView navigated to: {Url}, Result: {Result}", e.Url, e.Result);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.IsVisible = e.Result != WebNavigationResult.Success;
                }
            });

            // Log additional details if navigation failed
            if (e.Result != WebNavigationResult.Success)
            {
                _logger.LogWarning("WebView navigation failed with result: {Result}", e.Result);
                // The scene error will be triggered by WebViewService through OnSceneError
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnWebViewNavigated");
        }
    }

    private async void OnSceneReady(object sender, string message)
    {
        try
        {
            _logger.LogInformation("Scene ready event: {Message}", message);
            // Handle scene ready
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnSceneReady");
        }
    }

    private async void OnSceneError(object sender, string error)
    {
        try
        {
            _logger.LogError("Scene error: {Error}", error);
            // Handle scene error
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnSceneError");
        }
    }

    #endregion

    #region UI Event Handlers

    private async void OnMessageTextChanged(object sender, TextChangedEventArgs e)
    {
        // Debounce typing indicator
        // Implementation...
    }

    private async Task ShowErrorAndGoBack(string message)
    {
        // Show error and navigate back
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogDebug("ChatRoomPage disposing");

        _cts.Cancel();
        _cts.Dispose();

        // Unsubscribe WebView events
        if (AvatarWebView != null)
        {
            AvatarWebView.Navigated -= OnWebViewNavigated;
            AvatarWebView.Navigating -= OnWebViewNavigating;
        }

        // Unsubscribe WebViewService events
        _webViewService.OnSceneReady -= OnSceneReady;
        _webViewService.OnSceneError -= OnSceneError;

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogDebug("ChatRoomPage disposed");
    }

    #endregion
}
