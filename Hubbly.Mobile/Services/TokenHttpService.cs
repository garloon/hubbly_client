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
            _logger.LogInformation("Authenticating guest with avatar. BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
            var response = await _httpClient.PostAsJsonAsync("api/auth/guest-avatar", new { avatarConfig = avatarConfigJson });
            _logger.LogInformation("Response status: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result == null)
            {
                _logger.LogError("Empty response from server");
                throw new InvalidOperationException("Empty response from server");
            }
            _logger.LogInformation("Authentication successful. UserId: {UserId}, Nickname: {Nickname}", result.UserId, result.Nickname);
            return result;
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
            _logger.LogInformation("Refreshing token. BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
            var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", new { refreshToken, deviceId });
            _logger.LogInformation("Refresh response status: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result == null)
            {
                _logger.LogError("Empty response from server during refresh");
                throw new InvalidOperationException("Empty response from server");
            }
            _logger.LogInformation("Token refresh successful. UserId: {UserId}", result.UserId);
            return result;
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
            _logger.LogDebug("Checking server health at {BaseAddress}health/ready", _httpClient.BaseAddress);
            var response = await _httpClient.GetAsync("health/ready");
            _logger.LogInformation("Health check response: {StatusCode}", response.StatusCode);
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
        _logger.LogInformation("Waiting for server at {BaseAddress} (timeout: {Timeout}s)", _httpClient.BaseAddress, timeoutSeconds);

        while (DateTime.UtcNow - startTime < timeout && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var isHealthy = await CheckServerHealthAsync();
                if (isHealthy)
                {
                    _logger.LogInformation("Server is healthy after {Elapsed}s", (DateTime.UtcNow - startTime).TotalSeconds);
                    return true;
                }
            }
            catch { /* ignore */ }

            await Task.Delay(500, cancellationToken);
        }

        _logger.LogWarning("Server did not become healthy within {Timeout}s", timeoutSeconds);
        return false;
    }

    public async Task<bool> ConvertGuestToUserAsync(Guid guestUserId)
    {
        try
        {
            _logger.LogInformation("Converting guest user {UserId} to regular user", guestUserId);
            var response = await _httpClient.PostAsync($"api/users/{guestUserId}/convert-guest", null);
            _logger.LogInformation("Convert guest response: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert guest to user");
            throw;
        }
    }
}
