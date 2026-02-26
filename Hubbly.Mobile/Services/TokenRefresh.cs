using Hubbly.Mobile.Config;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Services;

public class TokenRefresh : ITokenRefresh
{
    private readonly ILogger<TokenRefresh> _logger;
    private readonly ITokenStorage _storage;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private Task<string>? _refreshTask;
    private bool _isRefreshing;

    public TokenRefresh(ITokenStorage storage, ILogger<TokenRefresh> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<string?> RefreshTokenAsync(AuthService authService)
    {
        try
        {
            _logger.LogInformation("TokenRefresh: Starting token refresh...");

            var refreshToken = await _storage.GetAsync("refresh_token");
            var deviceId = Preferences.Get("server_device_id",
                Preferences.Get("persistent_device_id", ""));

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("TokenRefresh: Missing refresh token");
                return null;
            }

            if (string.IsNullOrEmpty(deviceId))
            {
                _logger.LogWarning("TokenRefresh: Missing device ID");
                return null;
            }

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(AppConstants.TokenRefreshTimeout);

            var authResponse = await authService.RefreshTokenAsync(refreshToken, deviceId);

            // Save new tokens
            await _storage.SetAsync("access_token", authResponse.AccessToken, AppConstants.AccessTokenExpiration);
            await _storage.SetAsync("refresh_token", authResponse.RefreshToken, AppConstants.RefreshTokenExpiration);

            if (!string.IsNullOrEmpty(authResponse.DeviceId))
            {
                Preferences.Set("server_device_id", authResponse.DeviceId);
            }

            _logger.LogInformation("TokenRefresh: Token refreshed successfully");
            return authResponse.AccessToken;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("TokenRefresh: Refresh cancelled");
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "TokenRefresh: Refresh failed - unauthorized");
            // On critical auth error - clear everything
            await _storage.ClearAsync();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TokenRefresh: Refresh failed");
            return null;
        }
    }

    public async Task<string> GetValidTokenAsync(AuthService authService)
    {
        // Simple sequential synchronization
        if (_refreshTask != null)
        {
            return await _refreshTask; // Wait for current operation
        }

        await _refreshLock.WaitAsync();
        try
        {
            // Check again after acquiring lock
            var tokenInfo = await GetTokenInfoAsync();
            if (tokenInfo != null && !tokenInfo.IsExpired)
            {
                return tokenInfo.Value;
            }

            // Start refresh
            _isRefreshing = true;
            _refreshTask = RefreshTokenInternalAsync(authService);

            return await _refreshTask;
        }
        finally
        {
            _refreshLock.Release();
            _refreshTask = null;
            _isRefreshing = false;
        }
    }

    public async Task<bool> HasValidTokenAsync()
    {
        var tokenInfo = await GetTokenInfoAsync();
        return tokenInfo != null && !tokenInfo.IsExpired;
    }

    public async Task<TimeSpan?> GetTokenExpirationAsync(string key)
    {
        // Get expiration from storage directly
        var value = await _storage.GetAsync(key);
        
        // For simplicity, return null (this method is not critical)
        return null;
    }

    public async Task<string> GetNicknameAsync()
    {
        // Check cache first
        var cached = await _storage.GetAsync("nickname");
        if (!string.IsNullOrEmpty(cached))
            return cached;

        // Try Preferences as fallback
        var pref = await _storage.GetAsync("nickname");
        if (!string.IsNullOrEmpty(pref))
        {
            return pref;
        }

        return "Guest";
    }

    private async Task<TokenInfo?> GetTokenInfoAsync()
    {
        var value = await _storage.GetAsync("access_token");
        if (string.IsNullOrEmpty(value))
            return null;

        // Get expiration if available
        var expiresStr = Preferences.Get("access_token_expires", string.Empty);
        DateTimeOffset? expiresAt = null;
        if (!string.IsNullOrEmpty(expiresStr) && DateTimeOffset.TryParse(expiresStr, out var expires))
        {
            expiresAt = expires;
        }

        return new TokenInfo
        {
            Value = value,
            ExpiresAt = expiresAt
        };
    }

    private async Task<string> RefreshTokenInternalAsync(AuthService authService)
    {
        var result = await RefreshTokenAsync(authService);
        return result ?? throw new InvalidOperationException("Token refresh failed");
    }
}
