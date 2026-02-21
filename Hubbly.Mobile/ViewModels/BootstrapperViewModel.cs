using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hubbly.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Hubbly.Mobile.ViewModels;

public partial class BootstrapperViewModel : ObservableObject
{
    private readonly ILogger<BootstrapperViewModel> _logger;
    private readonly TokenManager _tokenManager;
    private readonly AuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly CancellationTokenSource _cts = new();
    private bool _isRunning;

    [ObservableProperty]
    private string _statusMessage = "Checking authentication...";

    public BootstrapperViewModel(
        ILogger<BootstrapperViewModel> logger,
        TokenManager tokenManager,
        AuthService authService,
        INavigationService navigationService)
    {
        _logger = logger;
        _tokenManager = tokenManager;
        _authService = authService;
        _navigationService = navigationService;

        _logger.LogInformation("BootstrapperViewModel created");
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;
        _isRunning = true;

        try
        {
            _logger.LogInformation("Bootstrapper starting...");

            // Step 1: Check if user has completed avatar selection
            var userId = await _tokenManager.GetAsync("user_id");
            
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("No user_id found - user needs to complete avatar selection");
                StatusMessage = "Setting up your avatar...";
                
                await Task.Delay(1000, _cts.Token);
                
                // Navigate to welcome page for first-time setup
                await _navigationService.NavigateToAsync("//welcome");
                return;
            }

            _logger.LogInformation("User ID found: {UserId}", userId);

            // Step 2: Validate tokens and refresh if needed
            StatusMessage = "Validating session...";
            
            var accessToken = await _tokenManager.GetAsync("access_token");
            var refreshToken = await _tokenManager.GetAsync("refresh_token");

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Missing tokens - clearing and redirecting to welcome");
                await ClearTokensAndRedirect();
                return;
            }

            // Step 3: Try to refresh the token if needed
            StatusMessage = "Refreshing session...";
            
            try
            {
                var refreshResult = await _authService.RefreshTokenAsync();
                if (!refreshResult.Success)
                {
                    _logger.LogWarning("Token refresh failed: {Error}", refreshResult.ErrorMessage);
                    await ClearTokensAndRedirect();
                    return;
                }

                // Save new tokens
                await _tokenManager.SetAsync("access_token", refreshResult.AccessToken);
                await _tokenManager.SetAsync("refresh_token", refreshResult.RefreshToken);
                
                _logger.LogInformation("Tokens refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh error");
                await ClearTokensAndRedirect();
                return;
            }

            // Step 4: Check server availability
            StatusMessage = "Connecting to server...";
            
            var isAvailable = await _authService.CheckServerHealthAsync();
            if (!isAvailable)
            {
                _logger.LogWarning("Server unavailable");
                await ShowErrorAndNavigate("Server is not responding. Please check your connection.");
                return;
            }

            // Step 5: All checks passed - navigate to chat
            StatusMessage = "Entering chat room...";
            _logger.LogInformation("Bootstrapper complete - navigating to chat room");

            await Task.Delay(500, _cts.Token);
            await _navigationService.NavigateToAsync("//chatroom");
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Bootstrapper cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bootstrapper error");
            await ShowErrorAndNavigate($"Initialization failed: {ex.Message}");
        }
        finally
        {
            _isRunning = false;
        }
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        await Task.CompletedTask;
    }

    private async Task ClearTokensAndRedirect()
    {
        _logger.LogInformation("Clearing tokens and redirecting to welcome");
        
        // Clear all stored auth data
        await _tokenManager.RemoveAsync("access_token");
        await _tokenManager.RemoveAsync("refresh_token");
        await _tokenManager.RemoveAsync("user_id");
        await _tokenManager.RemoveAsync("nickname");
        await _tokenManager.RemoveEncryptedAsync("avatar_config");
        await _tokenManager.RemoveEncryptedAsync("nickname");

        await _navigationService.NavigateToAsync("//welcome");
    }

    private async Task ShowErrorAndNavigate(string errorMessage)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Application.Current?.MainPage?.DisplayAlert(
                "Error",
                errorMessage,
                "OK");
            
            await _navigationService.NavigateToAsync("//welcome");
        });
    }
}
