using Hubbly.Mobile.Services;
using Hubbly.Mobile.ViewModels;
using Hubbly.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile;

public partial class App : Application, IDisposable
{
    private readonly ILogger<App> _logger;
    private readonly INavigationService _navigationService;
    private readonly SignalRService _signalRService;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;
    private bool _isSleeping;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _logger = serviceProvider.GetRequiredService<ILogger<App>>();
        _navigationService = serviceProvider.GetRequiredService<INavigationService>();
        _signalRService = serviceProvider.GetRequiredService<SignalRService>();

        _logger.LogInformation("App initializing...");

        // Setup main page
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
            var welcomePage = serviceProvider.GetRequiredService<WelcomePage>();
            var navigationPage = new NavigationPage(welcomePage);

            NavigationPage.SetHasNavigationBar(navigationPage.CurrentPage, false);
            navigationPage.BarBackgroundColor = Colors.Transparent;
            navigationPage.BarTextColor = Colors.Transparent;

            // Setup navigation error handling
            navigationPage.Popped += OnNavigationPopped;
            navigationPage.PoppedToRoot += OnNavigationPoppedToRoot;

            MainPage = navigationPage;

            _logger.LogInformation("Main page set to NavigationPage with WelcomePage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup main page");
            throw;
        }
    }

    #endregion

    #region Navigation Event Handlers

    private void OnNavigationPopped(object sender, NavigationEventArgs e)
    {
        _logger.LogDebug("Navigation popped: {PageType}", e.Page?.GetType().Name);

        // Clean up resources of leaving page if it's IDisposable
        if (e.Page is IDisposable disposablePage)
        {
            try
            {
                disposablePage.Dispose();
                _logger.LogTrace("Disposed page: {PageType}", e.Page.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing page: {PageType}", e.Page.GetType().Name);
            }
        }
    }

    private void OnNavigationPoppedToRoot(object sender, NavigationEventArgs e)
    {
        _logger.LogDebug("Navigation popped to root");
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

            if (currentPage is NavigationPage navPage)
            {
                _logger.LogDebug("Navigation stack size: {StackSize}",
                    navPage.Navigation.NavigationStack.Count);
            }

            // Check if we need to restore connection
            if (Current?.MainPage is NavigationPage { CurrentPage: ChatRoomPage })
            {
                _logger.LogDebug("On ChatRoomPage, checking connection...");

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
            if (Current?.MainPage is NavigationPage { CurrentPage: ChatRoomPage })
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
            // If we're in chat, reconnect
            if (Current?.MainPage is NavigationPage { CurrentPage: ChatRoomPage })
            {
                _logger.LogInformation("App resumed on ChatRoomPage, reconnecting...");

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

                    // If we're in chat - reconnect
                    if (Current?.MainPage is NavigationPage navPage &&
                        navPage.CurrentPage is ChatRoomPage chatPage &&
                        chatPage.BindingContext is ChatRoomViewModel viewModel)
                    {
                        if (!_signalRService.IsConnected)
                        {
                            _logger.LogInformation("Reconnecting to chat after network restore");
                            await viewModel.ConnectToChatCommand.ExecuteAsync(null);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("❌ Internet connection lost");

                    // Show notification if we're in chat
                    if (Current?.MainPage is NavigationPage { CurrentPage: ChatRoomPage })
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

        // Clear navigation events
        if (MainPage is NavigationPage navigationPage)
        {
            navigationPage.Popped -= OnNavigationPopped;
            navigationPage.PoppedToRoot -= OnNavigationPoppedToRoot;
        }

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