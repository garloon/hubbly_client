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

        InitializeWebView();

        _logger.LogInformation("ChatRoomPage created");
    }

    #region Initialization

    private void InitializeWebView()
    {
        try
        {
            if (AvatarWebView == null)
            {
                _logger.LogError("AvatarWebView is null");
                _viewModel.Disable3D();
                return;
            }

            // Подписываемся на события WebView
            AvatarWebView.Navigated += OnWebViewNavigated;

            // Инициализируем сервис WebView
            _webViewService.Initialize(AvatarWebView);
            _webViewService.OnSceneReady += OnSceneReady;
            _webViewService.OnSceneError += OnSceneError;

            _logger.LogDebug("WebView initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing WebView");
            _viewModel.Disable3D();
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
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            
            await _viewModel.OnAppearing();

            // Проверяем доступность сервера
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

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        _logger.LogDebug("ChatRoomPage disappearing");

        try
        {
            // Отменяем все операции
            _cts.Cancel();

            // Отписываемся от событий WebView
            UnsubscribeWebViewEvents();

            // Отключаемся от чата
            if (_viewModel.IsConnected)
            {
                await _viewModel.DisconnectFromChatCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnDisappearing");
        }
    }

    #endregion

    #region WebView Event Handlers

    private void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
    {
        try
        {
            _logger.LogDebug("WebView navigated to: {Url}, Result: {Result}", e.Url, e.Result);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.IsVisible = e.Result != WebNavigationResult.Success;
                }
            });
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

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.IsVisible = false;
                }

                _logger.LogInformation("✅ 3D Scene is ready");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnSceneReady");
        }
    }

    private async void OnSceneError(object sender, string message)
    {
        try
        {
            _logger.LogError("Scene error event: {Message}", message);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.IsVisible = false;
                }

                // Отключаем 3D при ошибке
                _viewModel.Disable3D();

                DisplayAlert("3D Error", "Failed to initialize 3D scene, using simple avatars", "OK");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnSceneError");
        }
    }

    private void UnsubscribeWebViewEvents()
    {
        try
        {
            if (AvatarWebView != null)
            {
                AvatarWebView.Navigated -= OnWebViewNavigated;
            }

            if (_webViewService != null)
            {
                _webViewService.OnSceneReady -= OnSceneReady;
                _webViewService.OnSceneError -= OnSceneError;
                _webViewService.Cleanup();
            }

            _logger.LogDebug("Unsubscribed from WebView events");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from WebView events");
        }
    }

    #endregion

    #region UI Event Handlers

    private async void OnMessageTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(e.NewTextValue))
            {
                if ((DateTime.Now - _lastTypingTime).TotalSeconds > 2)
                {
                    _lastTypingTime = DateTime.Now;

                    if (_viewModel.SendTypingIndicatorCommand.CanExecute(null))
                    {
                        await _viewModel.SendTypingIndicatorCommand.ExecuteAsync(null);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnMessageTextChanged");
        }
    }

    #endregion

    #region Helper Methods

    private async Task ShowErrorAndGoBack(string message)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await DisplayAlert("Error", message, "OK");
            await _navigationService.GoBackAsync();
        });
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("ChatRoomPage disposing...");

        // Отменяем все операции
        _cts.Cancel();
        _cts.Dispose();

        // Отписываемся от событий
        UnsubscribeWebViewEvents();

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("ChatRoomPage disposed");
    }

    #endregion
}