using Hubbly.Mobile.Config;
using Hubbly.Mobile.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Hubbly.Mobile.Services;

public class AuthService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly DeviceIdService _deviceIdService;
    private readonly ITokenStorage _tokenStorage;
    private readonly ITokenRefresh _tokenRefresh;
    private readonly ILogger<AuthService> _logger;
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private readonly string _apiBaseUrl;

    private bool _disposed;

    public AuthService(
        HttpClient httpClient,
        DeviceIdService deviceIdService,
        ITokenStorage tokenStorage,
        ITokenRefresh tokenRefresh,
        ILogger<AuthService> logger)
    {
        _deviceIdService = deviceIdService ?? throw new ArgumentNullException(nameof(deviceIdService));
        _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
        _tokenRefresh = tokenRefresh ?? throw new ArgumentNullException(nameof(tokenRefresh));
        _logger = logger;

        // Get server URL from Preferences via ServerConfig
        _apiBaseUrl = ServerConfig.GetServerUrl();

        // Configure HttpClient
        _httpClient = ConfigureHttpClient(httpClient);
    }

    #region Public Methods

    public async Task<AuthResponse> AuthenticateGuestWithAvatarAsync(string avatarConfigJson)
    {
        ThrowIfDisposed();

        // Validate avatar config JSON if provided
        if (!string.IsNullOrEmpty(avatarConfigJson))
        {
            try
            {
                var config = AvatarConfigDto.FromJson(avatarConfigJson);
                if (config == null)
                {
                    throw new ArgumentException("Invalid avatar config JSON");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "AuthService: Invalid avatar config JSON");
                throw new ArgumentException("Avatar config JSON is malformed", ex);
            }
        }
        else
        {
            // Use default male config if none provided
            avatarConfigJson = AvatarConfigDto.DefaultMale.ToJson();
        }

        // Get or create device ID
        var deviceId = await GetOrCreateDeviceIdAsync();

        try
        {
            _logger.LogInformation("AuthService: Authenticating guest with avatar...");

            var request = new
            {
                avatarConfig = avatarConfigJson,
                deviceId = deviceId
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/guest-avatar", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("AuthService: Guest auth failed - {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>()
                ?? throw new InvalidOperationException("Empty response from server");

            // Save tokens
            await _tokenStorage.SetAsync("access_token", authResponse.AccessToken, AppConstants.AccessTokenExpiration);
            await _tokenStorage.SetAsync("refresh_token", authResponse.RefreshToken, AppConstants.RefreshTokenExpiration);
            await _tokenStorage.SetAsync("user_id", authResponse.UserId.ToString());
            await _tokenStorage.SetAsync("nickname", authResponse.Nickname);
            await _tokenStorage.SetAsync("device_id", deviceId);

            // Save device ID in Preferences for later use
            Preferences.Set("server_device_id", deviceId);
            Preferences.Set("persistent_device_id", _deviceIdService.GetPersistentDeviceId());

            // Save token expiration if provided
            if (authResponse.ExpiresAt.HasValue)
            {
                Preferences.Set("access_token_expires", authResponse.ExpiresAt.Value.ToString("o"));
            }

            _logger.LogInformation("AuthService: Guest authenticated successfully. UserId: {UserId}", authResponse.UserId);
            return authResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuthService: Guest authentication failed");
            throw;
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string deviceId)
    {
        ThrowIfDisposed();

        // This method is now DEPRECATED - use ITokenRefresh directly
        // Kept for backward compatibility during transition
        _logger.LogWarning("AuthService.RefreshTokenAsync is deprecated. Use ITokenRefresh directly.");
        
        var result = await _tokenRefresh.RefreshTokenAsync();
        if (result == null)
            throw new InvalidOperationException("Token refresh failed");

        // Need to get full AuthResponse - this is a problem
        // Better to use ITokenRefresh exclusively
        throw new NotSupportedException("Use ITokenRefresh.GetValidTokenAsync() instead");
    }

    public async Task<bool> CheckServerHealthAsync()
    {
        ThrowIfDisposed();

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
        ThrowIfDisposed();

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

    public async Task<string?> GetCurrentUserIdAsync()
    {
        var userIdStr = await _tokenStorage.GetAsync("user_id");
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return null;
        }
        return userId.ToString();
    }

    #endregion

    #region Private Helpers

    private async Task<string> GetOrCreateDeviceIdAsync()
    {
        // Check existing device ID
        var existingDeviceId = await _tokenStorage.GetAsync("device_id");
        if (!string.IsNullOrEmpty(existingDeviceId))
        {
            return existingDeviceId;
        }

        // Get from Preferences
        var prefDeviceId = Preferences.Get("server_device_id", null);
        if (!string.IsNullOrEmpty(prefDeviceId))
        {
            return prefDeviceId;
        }

        // Generate new device ID
        var newDeviceId = _deviceIdService.GetPersistentDeviceId();
        await _tokenStorage.SetAsync("device_id", newDeviceId);
        Preferences.Set("server_device_id", newDeviceId);
        Preferences.Set("persistent_device_id", _deviceIdService.GetPersistentDeviceId());

        return newDeviceId;
    }

    private HttpClient ConfigureHttpClient(HttpClient httpClient)
    {
        httpClient.BaseAddress = new Uri(_apiBaseUrl);
        httpClient.DefaultRequestHeaders.Accept.Add(new("application/json"));
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"HubblyMobile/{AppInfo.Current.VersionString} ({DeviceInfo.Current.Platform}; {DeviceInfo.Current.VersionString})");

        return httpClient;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AuthService));
        }
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _authLock?.Dispose();
            }
            _disposed = true;
        }
    }
}
