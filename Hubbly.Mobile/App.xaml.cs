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

        // Настраиваем главную страницу
        SetupMainPage(serviceProvider);

        // Подписываемся на события подключения
        Connectivity.ConnectivityChanged += OnConnectivityChanged;

        // Подписываемся на события приложения
        RequestedThemeChanged += OnRequestedThemeChanged;

        _logger.LogInformation("App initialized");
    }

    #region Инициализация

    private void SetupMainPage(IServiceProvider serviceProvider)
    {
        try
        {
            var welcomePage = serviceProvider.GetRequiredService<WelcomePage>();
            var navigationPage = new NavigationPage(welcomePage);

            NavigationPage.SetHasNavigationBar(navigationPage.CurrentPage, false);
            navigationPage.BarBackgroundColor = Colors.Transparent;
            navigationPage.BarTextColor = Colors.Transparent;

            // Настраиваем обработку навигационных ошибок
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

    #region Обработчики событий навигации

    private void OnNavigationPopped(object sender, NavigationEventArgs e)
    {
        _logger.LogDebug("Navigation popped: {PageType}", e.Page?.GetType().Name);

        // Очищаем ресурсы уходящей страницы если она IDisposable
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

    #region Обработчики событий приложения

    protected override async void OnStart()
    {
        base.OnStart();

        _logger.LogInformation("App OnStart");
        _isSleeping = false;

        try
        {
            // Логируем текущую страницу
            var currentPage = Current?.MainPage;
            _logger.LogDebug("Current MainPage type: {PageType}", currentPage?.GetType().Name);

            if (currentPage is NavigationPage navPage)
            {
                _logger.LogDebug("Navigation stack size: {StackSize}",
                    navPage.Navigation.NavigationStack.Count);
            }

            // Проверяем, нужно ли восстановить соединение
            if (Current?.MainPage is NavigationPage { CurrentPage: ChatRoomPage })
            {
                _logger.LogDebug("On ChatRoomPage, checking connection...");

                // Проверяем соединение и восстанавливаем если нужно
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
            // Если мы в чате, отключаемся для экономии ресурсов
            if (Current?.MainPage is NavigationPage { CurrentPage: ChatRoomPage })
            {
                _logger.LogInformation("App sleeping on ChatRoomPage, disconnecting...");

                // Даем время на отправку последних сообщений
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
            // Если мы в чате, переподключаемся
            if (Current?.MainPage is NavigationPage { CurrentPage: ChatRoomPage })
            {
                _logger.LogInformation("App resumed on ChatRoomPage, reconnecting...");

                await Task.Delay(1000); // Даем время на восстановление сети

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

        if (_isSleeping) return; // Не обрабатываем, если приложение в фоне

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (e.NetworkAccess == NetworkAccess.Internet)
                {
                    _logger.LogInformation("✅ Internet connection restored");

                    // Если мы в чате - переподключаемся
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

                    // Показываем уведомление если мы в чате
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
        // Можно добавить логику смены темы
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("App disposing...");

        _cts.Cancel();
        _cts.Dispose();

        // Отписываемся от событий
        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        RequestedThemeChanged -= OnRequestedThemeChanged;

        // Очищаем навигационные события
        if (MainPage is NavigationPage navigationPage)
        {
            navigationPage.Popped -= OnNavigationPopped;
            navigationPage.PoppedToRoot -= OnNavigationPoppedToRoot;
        }

        // Останавливаем SignalR
        _signalRService?.Dispose();

        // Очищаем MainPage
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