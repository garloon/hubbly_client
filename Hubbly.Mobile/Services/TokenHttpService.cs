using System.Net.Http.Json;
using Hubbly.Mobile.Config;
using Hubbly.Mobile.Models;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Services;

public class TokenHttpService : ITokenHttpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TokenHttpService> _logger;

    public TokenHttpService(HttpClient httpClient, ILogger<TokenHttpService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AuthResponse> AuthenticateGuestWithAvatarAsync(string avatarConfigJson)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/guest-avatar", new { avatarConfig = avatarConfigJson });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AuthResponse>() 
                   ?? throw new InvalidOperationException("Empty response from server");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate guest with avatar");
            throw;
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string deviceId)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", new { refreshToken, deviceId });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AuthResponse>()
                   ?? throw new InvalidOperationException("Empty response from server");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token");
            throw;
        }
    }

    public async Task<bool> CheckServerHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Server health check failed");
            return false;
        }
    }

    public async Task<bool> WaitForServerAsync(int timeoutSeconds = 10, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        while (DateTime.UtcNow - startTime < timeout && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var isHealthy = await CheckServerHealthAsync();
                if (isHealthy) return true;
            }
            catch { /* ignore */ }

            await Task.Delay(500, cancellationToken);
        }

        return false;
    }

    public async Task<bool> ConvertGuestToUserAsync(Guid guestUserId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/users/{guestUserId}/convert-guest", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert guest to user");
            throw;
        }
    }
}
