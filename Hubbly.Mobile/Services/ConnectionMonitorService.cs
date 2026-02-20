using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Services;

public class ConnectionMonitorService : BackgroundService
{
    private readonly ILogger<ConnectionMonitorService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public ConnectionMonitorService(
        ILogger<ConnectionMonitorService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Connection monitor service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckConnections(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in connection monitor");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Connection monitor service stopped");
    }

    private async Task CheckConnections(CancellationToken cancellationToken)
    {
        // Проверяем состояние SignalR
        using var scope = _serviceProvider.CreateScope();

        var signalRService = scope.ServiceProvider.GetRequiredService<SignalRService>();
        var navigationService = scope.ServiceProvider.GetRequiredService<INavigationService>();

        // Получаем текущую страницу
        var currentPage = await navigationService.GetCurrentPageAsync();

        // Если мы в чате, проверяем соединение
        if (currentPage?.BindingContext is ViewModels.ChatRoomViewModel chatViewModel)
        {
            if (!signalRService.IsConnected && Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                _logger.LogWarning("Connection lost in chat, attempting to reconnect...");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        await chatViewModel.ConnectToChatCommand.ExecuteAsync(null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to reconnect from monitor");
                    }
                });
            }
        }
    }
}