using Android.Telephony;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hubbly.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.ViewModels;

public partial class WelcomeViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<WelcomeViewModel> _logger;
    private readonly INavigationService _navigationService;
    private readonly TokenManager _tokenManager;
    private readonly DeviceIdService _deviceIdService;
    private readonly AuthService _authService;
    private readonly SemaphoreSlim _navigationLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();

    private bool _disposed;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public WelcomeViewModel(
        INavigationService navigationService,
        TokenManager tokenManager,
        DeviceIdService deviceIdService,
        AuthService authService,
        ILogger<WelcomeViewModel> logger)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
        _deviceIdService = deviceIdService ?? throw new ArgumentNullException(nameof(deviceIdService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        _logger = logger;

        _logger.LogInformation("WelcomeViewModel created");
    }

    #region Команды

    [RelayCommand]
    private async Task EnterAsGuest()
    {
        if (IsBusy)
        {
            _logger.LogWarning("EnterAsGuest: Already busy");
            return;
        }

        if (!await _navigationLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            _logger.LogError("EnterAsGuest: Failed to acquire lock");
            StatusMessage = "System busy, please try again";
            HasError = true;
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Connecting to server...";
            HasError = false;

            _logger.LogInformation("EnterAsGuest started");

            // Проверяем сервер с таймаутом
            var isServerHealthy = await _authService.WaitForServerAsync(5, _cts.Token);

            if (!isServerHealthy)
            {
                StatusMessage = "Cannot connect to Hubbly server. Please check your internet connection.";
                HasError = true;
                _logger.LogWarning("Server unavailable");
                return;
            }

            StatusMessage = "Checking existing session...";

            // Получаем device ID
            var deviceId = _deviceIdService.GetPersistentDeviceId();
            _logger.LogDebug("Device ID: {DeviceId}", deviceId);

            // Проверяем, есть ли валидный токен
            StatusMessage = "Restoring session...";
            var tokenValid = await _tokenManager.GetValidTokenAsync(_authService);

            if (!string.IsNullOrEmpty(tokenValid))
            {
                // Есть валидная сессия - идем сразу в чат
                _logger.LogInformation("Valid token found, navigating to chat");
                StatusMessage = "Restoring your session...";
                await _navigationService.NavigateToAsync("//ChatRoomPage");
            }
            else
            {
                // Нет сессии - идем создавать аватар
                _logger.LogInformation("No valid token, navigating to avatar selection");
                StatusMessage = "Creating new avatar...";
                await _navigationService.NavigateToAsync("//AvatarSelectionPage");
            }

            _logger.LogInformation("EnterAsGuest completed successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("EnterAsGuest cancelled");
            StatusMessage = "Operation cancelled";
            HasError = true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error in EnterAsGuest");
            StatusMessage = "Network error. Please check your connection.";
            HasError = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in EnterAsGuest");
            StatusMessage = $"Error: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsBusy = false;
            _navigationLock.Release();
        }
    }

    [RelayCommand]
    private void ClearError()
    {
        HasError = false;
        StatusMessage = string.Empty;
    }

    #endregion

    #region Жизненный цикл

    public void OnAppearing()
    {
        _logger.LogDebug("WelcomePage appearing");
        ClearError();
    }

    public void OnDisappearing()
    {
        _logger.LogDebug("WelcomePage disappearing");
        _cts.Cancel();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("WelcomeViewModel disposing...");

        _cts.Cancel();
        _cts.Dispose();
        _navigationLock.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("WelcomeViewModel disposed");
    }

    #endregion
}