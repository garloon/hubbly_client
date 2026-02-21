using Hubbly.Mobile.Services;
using Hubbly.Mobile.ViewModels;
using Hubbly.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile;

public partial class App : Application, IDisposable
{
    private readonly ILogger<App> _logger;
    private readonly SignalRService _signalRService;
    private readonly TokenManager _tokenManager;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;
    private bool _isSleeping;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _logger = serviceProvider.GetRequiredService<ILogger<App>>();
        _signalRService = serviceProvider.GetRequiredService<SignalRService>();
        _tokenManager = serviceProvider.GetRequiredService<TokenManager>();

        _logger.LogInformation("App initializing...");

        // Setup main page with Shell
        SetupMainPage(serviceProvider);

        // Subscribe to connectivity events
        Connectivity.ConnectivityChanged += OnConnectivityChanged;

        // Subscribe to application events
        RequestedThemeChanged += OnRequestedThemeChanged;

        _logger.LogInformation("App initialized");
    }

    #region Initialization

    private void SetupMainPage(IServiceProvider serviceProvider)
    {
        try
        {
            // Use AppShell as main page
            var appShell = serviceProvider.GetRequiredService<AppShell>();
            MainPage = appShell;

            _logger.LogInformation("Main page set to AppShell");

            // Navigate to welcome page as initial route (after a short delay to ensure Shell is ready)
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                if (Shell.Current?.CurrentPage is not WelcomePage)
                {
                    await Shell.Current.GoToAsync("//welcome");
                    _logger.LogInformation("Navigated to welcome page as initial route");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup main page");
            throw;
        }
    }

    #endregion

    #region Application Event Handlers

    protected override async void OnStart()
    {
        base.OnStart();

        _logger.LogInformation("App OnStart");
        _isSleeping = false;

        try
        {
            // Log current page
            var currentPage = Current?.MainPage;
            _logger.LogDebug("Current MainPage type: {PageType}", currentPage?.GetType().Name);

            // Check if we need to restore connection ONLY if user has completed avatar selection
            // (has user_id stored) and we're on ChatRoomPage
            if (Current?.MainPage is Shell shell && shell.CurrentPage is ChatRoomPage)
            {
                // Check if user has completed avatar selection (has user_id)
                var userId = await _tokenManager.GetAsync("user_id");
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("App OnStart: No user_id found - user hasn't completed avatar selection. Skipping SignalR connection.");
                    return;
                }

                _logger.LogDebug("On ChatRoomPage with valid user, checking connection...");

                // Check connection and restore if needed
                await Task.Delay(1000);

                if (!_signalRService.IsConnected && Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    _logger.LogInformation("Reconnecting to SignalR after app start");
                    await _signalRService.StartConnection();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnStart");
        }
    }

    protected override async void OnSleep()
    {
        base.OnSleep();

        _logger.LogInformation("App OnSleep");
        _isSleeping = true;

        try
        {
            // If we're in chat, disconnect to save resources
            if (Current?.MainPage is Shell { CurrentPage: ChatRoomPage })
            {
                _logger.LogInformation("App sleeping on ChatRoomPage, disconnecting...");

                // Give time to send pending messages
                await Task.Delay(500);

                await _signalRService.StopConnection();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnSleep");
        }
    }

    protected override async void OnResume()
    {
        base.OnResume();

        _logger.LogInformation("App OnResume");
        _isSleeping = false;

        try
        {
            // If we're in chat, reconnect ONLY if user has completed avatar selection
            if (Current?.MainPage is Shell { CurrentPage: ChatRoomPage })
            {
                // Check if user has completed avatar selection (has user_id)
                var userId = await _tokenManager.GetAsync("user_id");
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("App OnResume: No user_id found - user hasn't completed avatar selection. Skipping SignalR connection.");
                    return;
                }

                _logger.LogInformation("App resumed on ChatRoomPage with valid user, reconnecting...");

                await Task.Delay(1000); // Give time for network to restore

                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    await _signalRService.StartConnection();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnResume");
        }
    }

    private async void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        _logger.LogInformation("Network connectivity changed: {NetworkAccess}", e.NetworkAccess);

        if (_isSleeping) return; // Don't process if app is in background

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (e.NetworkAccess == NetworkAccess.Internet)
                {
                    _logger.LogInformation("✅ Internet connection restored");

                    // If we're in chat - reconnect ONLY if user has completed avatar selection
                    if (Current?.MainPage is Shell shell &&
                        shell.CurrentPage is ChatRoomPage chatPage)
                    {
                        // Check if user has completed avatar selection (has user_id)
                        var userId = await _tokenManager.GetAsync("user_id");
                        if (string.IsNullOrEmpty(userId))
                        {
                            _logger.LogWarning("OnConnectivityChanged: No user_id found - skipping SignalR connection.");
                            return;
                        }

                        if (!_signalRService.IsConnected)
                        {
                            _logger.LogInformation("Reconnecting to chat after network restore");
                            if (chatPage.BindingContext is ChatRoomViewModel viewModel)
                            {
                                await viewModel.ConnectToChatCommand.ExecuteAsync(null);
                            }
                            else
                            {
                                await _signalRService.StartConnection();
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("❌ Internet connection lost");

                    // Show notification if we're in chat
                    if (Current?.MainPage is Shell { CurrentPage: ChatRoomPage })
                    {
                        await Current.MainPage.DisplayAlert(
                            "Connection Lost",
                            "Internet connection lost. Reconnecting...",
                            "OK");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling connectivity change");
        }
    }

    private void OnRequestedThemeChanged(object sender, AppThemeChangedEventArgs e)
    {
        _logger.LogInformation("App theme changed to: {RequestedTheme}", e.RequestedTheme);
        // Can add theme change logic here
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("App disposing...");

        _cts.Cancel();
        _cts.Dispose();

        // Unsubscribe from events
        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        RequestedThemeChanged -= OnRequestedThemeChanged;

        // Clear navigation events (not needed with Shell)
        // Shell handles navigation automatically

        // Stop SignalR
        _signalRService?.Dispose();

        // Clear MainPage
        if (MainPage is IDisposable disposablePage)
        {
            disposablePage.Dispose();
        }
        MainPage = null;

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("App disposed");
    }

    #endregion
}