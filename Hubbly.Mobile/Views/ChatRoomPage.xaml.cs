using Android.Runtime;
using Hubbly.Mobile.Services;
using Hubbly.Mobile.ViewModels;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Hubbly.Mobile.Models;
using System.Collections.ObjectModel;

namespace Hubbly.Mobile.Views;

public partial class ChatRoomPage : ContentPage, IDisposable
{
    private ILogger<ChatRoomPage> _logger;
    private readonly ChatRoomViewModel _viewModel;
    private WebViewService _webViewService;
    private AuthService _authService;
    private INavigationService _navigationService;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;
    private DateTime _lastTypingTime = DateTime.MinValue;

    public ChatRoomPage(
        ChatRoomViewModel viewModel,
        ILogger<ChatRoomPage> logger)
    {
        try
        {
            // 1. Сначала базовые присвоения
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            // 2. InitializeComponent с отдельным try-catch для диагностики
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== XAML INITIALIZATION ERROR ===");
                System.Diagnostics.Debug.WriteLine($"Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // Пробуем показать пользователю
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Page Error",
                        $"Failed to load chat page: {ex.Message}",
                        "OK");
                });
                throw;
            }

            // 3. После успешной инициализации XAML устанавливаем BindingContext
            BindingContext = _viewModel;

            // 4. Логгер (может быть null на этом этапе, но проверим)
            _logger = logger;

            // 5. Получаем сервисы через ServiceProvider с проверками
            if (MauiProgram.ServiceProvider == null)
            {
                throw new InvalidOperationException("MauiProgram.ServiceProvider is null! App not properly initialized.");
            }

            // Получаем сервисы по одному с логированием
            System.Diagnostics.Debug.WriteLine("Getting WebViewService...");
            _webViewService = MauiProgram.ServiceProvider.GetService<WebViewService>();
            if (_webViewService == null)
            {
                _webViewService = MauiProgram.ServiceProvider.GetRequiredService<WebViewService>(); // Это выбросит исключение если сервис не зарегистрирован
            }
            System.Diagnostics.Debug.WriteLine("✓ WebViewService obtained");

            System.Diagnostics.Debug.WriteLine("Getting AuthService...");
            _authService = MauiProgram.ServiceProvider.GetService<AuthService>();
            if (_authService == null)
            {
                _authService = MauiProgram.ServiceProvider.GetRequiredService<AuthService>();
            }
            System.Diagnostics.Debug.WriteLine("✓ AuthService obtained");

            System.Diagnostics.Debug.WriteLine("Getting INavigationService...");
            _navigationService = MauiProgram.ServiceProvider.GetService<INavigationService>();
            if (_navigationService == null)
            {
                _navigationService = MauiProgram.ServiceProvider.GetRequiredService<INavigationService>();
            }
            System.Diagnostics.Debug.WriteLine("✓ INavigationService obtained");

            // 6. Теперь можно использовать логгер (он уже должен быть)
            _logger?.LogInformation("ChatRoomPage constructor started");

            NavigationPage.SetHasNavigationBar(this, false);

            // Initialize WebView (subscribe to events) before setting source
            InitializeWebView();

            // Initialize CollectionView for auto-scrolling
            InitializeCollectionView();

            // Set WebView source dynamically based on server URL from Preferences
            var serverUrl = Preferences.Get("server_url", "http://89.169.46.33:5000");
            var webViewUrl = $"{serverUrl.TrimEnd('/')}/three_scene.html";
            _logger?.LogInformation("Setting WebView source to: {Url}", webViewUrl);

            AvatarWebView.Source = webViewUrl;
            _logger?.LogInformation("WebView source set successfully");

            _logger?.LogInformation("ChatRoomPage created successfully");

            // Subscribe to orientation changes for adaptive layout
            DeviceDisplay.MainDisplayInfoChanged += OnMainDisplayInfoChanged;
        }
        catch (Exception ex)
        {
            // Диагностика ошибки
            System.Diagnostics.Debug.WriteLine($"=== CRITICAL ERROR IN ChatRoomPage CONSTRUCTOR ===");
            System.Diagnostics.Debug.WriteLine($"Type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }

            // Пробуем показать пользователю
            try
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Critical Error",
                        $"Failed to initialize chat: {ex.Message}",
                        "OK");
                });
            }
            catch { /* Игнорируем ошибки показа алерта */ }

            throw; // Re-throw so global handler can catch it
        }
    }

    #region Initialization

    private void InitializeWebView()
    {
        try
        {
            if (AvatarWebView == null)
            {
                _logger?.LogError("AvatarWebView is null - 3D will be disabled");
                _viewModel.Disable3D();
                return;
            }

            // Log WebView source URL for debugging
            _logger?.LogInformation("WebView source URL: {Source}", AvatarWebView.Source?.ToString() ?? "null");

            // Subscribe to WebView events
            AvatarWebView.Navigated += OnWebViewNavigated;
            AvatarWebView.Navigating += OnWebViewNavigating;

            // Initialize WebView service
            _webViewService.Initialize(AvatarWebView);
            _webViewService.OnSceneReady += OnSceneReady;
            _webViewService.OnSceneError += OnSceneError;

            _logger?.LogInformation("WebView initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error initializing WebView");
            _viewModel.Disable3D();
        }
    }

    private void InitializeCollectionView()
    {
        try
        {
            if (_viewModel?.Messages != null && !_disposed)
            {
                _viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
                _logger?.LogInformation("CollectionView initialization complete");
            }
            else
            {
                _logger?.LogWarning("Cannot initialize CollectionView: ViewModel or Messages is null, or page is disposed");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error initializing CollectionView");
        }
    }

    private void OnMessagesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        const int maxAttempts = 3;

        try
        {
            // Don't process if page is disposed or view is null
            if (_disposed || MessagesCollectionView == null || _viewModel == null)
                return;

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems?.Count > 0)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        if (_disposed || MessagesCollectionView == null || _viewModel?.Messages == null)
                            return;

                        var messages = _viewModel.Messages;
                        if (messages.Count == 0)
                            return;

                        // Get the target index
                        int targetIndex = messages.Count - 1;

                        // Wait for UI to process the collection change
                        await Task.Delay(100);

                        // Re-check after delay
                        if (_disposed || MessagesCollectionView == null || _viewModel?.Messages == null)
                            return;

                        // Verify the index is still valid
                        if (targetIndex < 0 || targetIndex >= _viewModel.Messages.Count)
                            return;

                        // Try scrolling with retry logic
                        int attempts = 0;

                        while (attempts < maxAttempts)
                        {
                            try
                            {
                                MessagesCollectionView.ScrollTo(targetIndex, ScrollToPosition.End);
                                break; // Success, exit loop
                            }
                            catch (Exception ex) when (attempts < maxAttempts - 1)
                            {
                                attempts++;
                                _logger?.LogDebug(ex, "ScrollTo attempt {Attempt} failed, retrying...", attempts);
                                await Task.Delay(100); // Wait before retry
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug(ex, "ScrollTo failed after {Attempts} attempts", maxAttempts);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - collection changed events should be safe
            _logger?.LogDebug(ex, "Error handling messages collection changed");
        }
    }

    private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
    {
        try
        {
            _logger?.LogInformation("WebView navigating to: {Url}", e.Url);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in OnWebViewNavigating");
        }
    }

    #endregion

    #region Lifecycle

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _logger?.LogDebug("ChatRoomPage appearing");

        // Check if page is already disposed
        if (_disposed)
        {
            _logger?.LogWarning("ChatRoomPage appearing after disposed - ignoring");
            return;
        }

        try
        {
            // Re-initialize WebView source if it was reset in OnDisappearing
            if (AvatarWebView != null && AvatarWebView.Source == null)
            {
                _logger?.LogInformation("Re-initializing WebView source after cache clear");
                var serverUrl = Preferences.Get("server_url", "http://89.169.46.33:5000");
                AvatarWebView.Source = $"{serverUrl.TrimEnd('/')}/three_scene.html";
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            await _viewModel.OnAppearing();

            // Fade-in animation
            if (MainGrid != null && !_disposed)
            {
                MainGrid.Opacity = 0;
                await MainGrid.FadeTo(1, 500, Easing.CubicInOut);
            }

            // Check server availability
            var isAvailable = await _authService.CheckServerHealthAsync();

            if (!isAvailable && !_disposed)
            {
                _logger?.LogWarning("Server unavailable");
                await ShowErrorAndGoBack("Chat server is not responding. Please try again later.");
                return;
            }
        }
        catch (OperationCanceledException)
        {
            if (!_disposed)
            {
                _logger?.LogWarning("Connection timeout");
                await ShowErrorAndGoBack("Connection timeout. Please try again.");
            }
        }
        catch (Exception ex)
        {
            if (!_disposed)
            {
                _logger?.LogError(ex, "Error in OnAppearing");
                await ShowErrorAndGoBack("Failed to connect to chat server.");
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _logger?.LogDebug("ChatRoomPage disappearing");

        // IMPORTANT: Do NOT clear the 3D scene or disconnect from chat here.
        // When navigating to modal pages (Settings/About), ChatRoomPage remains visible
        // underneath, so we must preserve the scene state. Also, when returning from
        // modal pages, we want the scene to be intact.
        //
        // Cleanup (scene clearing, chat disconnection) is performed only in:
        // - DisposeAsync() when the page is permanently destroyed
        // - LogoutCommand when user explicitly logs out

        _cts.Cancel(); // Only cancel operations, preserve scene

        _logger?.LogInformation("ChatRoomPage OnDisappearing: cancelled operations, scene preserved");
    }

    #endregion

    #region WebView Event Handlers

    private void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
    {
        try
        {
            // Handle WebView navigation completion if needed
            _logger?.LogDebug("WebView navigated to: {Url}", e.Url);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in OnWebViewNavigated");
        }
    }

    private async void OnSceneReady(object sender, string message)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!_disposed && LoadingOverlay != null)
                {
                    LoadingOverlay.IsVisible = false;
                    _logger?.LogInformation("3D scene ready: {Message}", message);
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling scene ready");
        }
    }

    private async void OnSceneError(object sender, string error)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!_disposed && LoadingOverlay != null)
                {
                    LoadingOverlay.IsVisible = false;
                    _logger?.LogError("3D scene error: {Error}", error);
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling scene error");
        }
    }

    #endregion

    #region Orientation Handling

    private void OnMainDisplayInfoChanged(object sender, DisplayInfoChangedEventArgs e)
    {
        try
        {
            var orientation = DeviceDisplay.Current.MainDisplayInfo.Orientation;
            _logger?.LogDebug("Orientation changed to: {Orientation}", orientation);

            // Adjust layout based on orientation
            AdjustLayoutForOrientation(orientation);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling orientation change");
        }
    }

    private void AdjustLayoutForOrientation(DisplayOrientation orientation)
    {
        try
        {
            var isLandscape = orientation == DisplayOrientation.Landscape;

            // Adjust WebView and fallback UI proportions
            // In portrait: 3D scene takes ~60% of screen
            // In landscape: 3D scene takes ~70% of screen (more horizontal space)
            if (isLandscape)
            {
                // Landscape: wider layout, adjust paddings and sizes
                // The Grid with Auto,* will handle most of this automatically
                _logger?.LogDebug("Landscape layout applied");
            }
            else
            {
                // Portrait: taller layout
                _logger?.LogDebug("Portrait layout applied");
            }

            // Force layout update
            InvalidateMeasure();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adjusting layout for orientation");
        }
    }

    #endregion

    #region Helpers

    private async Task ShowErrorAndGoBack(string message)
    {
        try
        {
            await DisplayAlert("Error", message, "OK");
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing error message");
        }
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        if (_disposed) return;

        _logger?.LogInformation("ChatRoomPage disposing...");

        try
        {
            // Mark as disposed first to prevent further event processing
            _disposed = true;

            // Unsubscribe from events
            DeviceDisplay.MainDisplayInfoChanged -= OnMainDisplayInfoChanged;

            if (AvatarWebView != null)
            {
                AvatarWebView.Navigated -= OnWebViewNavigated;
                AvatarWebView.Navigating -= OnWebViewNavigating;
            }

            if (_webViewService != null)
            {
                _webViewService.OnSceneReady -= OnSceneReady;
                _webViewService.OnSceneError -= OnSceneError;
            }

            // Unsubscribe from messages collection changed
            if (_viewModel?.Messages != null)
            {
                try
                {
                    _viewModel.Messages.CollectionChanged -= OnMessagesCollectionChanged;
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Error unsubscribing from Messages.CollectionChanged (may already be unsubscribed)");
                }
            }

            // Cancel operations
            _cts?.Cancel();
            _cts?.Dispose();

            GC.SuppressFinalize(this);

            _logger?.LogInformation("ChatRoomPage disposed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during ChatRoomPage disposal");
        }
    }

    #endregion
}