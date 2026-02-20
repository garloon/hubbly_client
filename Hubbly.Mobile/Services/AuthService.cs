using Hubbly.Mobile.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Hubbly.Mobile.Services;

public class AuthService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly DeviceIdService _deviceIdService;
    private readonly TokenManager _tokenManager;
    private readonly ILogger<AuthService> _logger;
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();

    private bool _disposed;

    public AuthService(HttpClient httpClient, DeviceIdService deviceIdService, TokenManager tokenManager, ILogger<AuthService> logger)
    {
        _deviceIdService = deviceIdService ?? throw new ArgumentNullException(nameof(deviceIdService));
        _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
        _logger = logger;

        // Настраиваем HttpClient
        _httpClient = ConfigureHttpClient(httpClient);
    }

    #region Публичные методы

    public async Task<AuthResponse> AuthenticateGuestWithAvatarAsync(string avatarConfigJson)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(avatarConfigJson))
        {
            avatarConfigJson = new AvatarConfigDto { Gender = "male" }.ToJson();
        }

        if (!await _authLock.WaitAsync(TimeSpan.FromSeconds(10)))
        {
            _logger.LogError("AuthService: Failed to acquire lock within 10 seconds");
            throw new TimeoutException("Authentication service is busy");
        }

        try
        {
            _logger.LogInformation("AuthService: Starting guest authentication");

            var deviceId = _deviceIdService.GetPersistentDeviceId();

            var request = new
            {
                DeviceId = deviceId,
                AvatarConfigJson = avatarConfigJson
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(15)); // Таймаут 15 секунд

            var response = await _httpClient.PostAsync("api/auth/guest", content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogError("AuthService: Authentication failed - {StatusCode} - {Error}",
                    response.StatusCode, error);

                throw new HttpRequestException($"Authentication failed: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, options);

            if (authResponse == null)
            {
                throw new Exception("Failed to deserialize auth response");
            }

            // Сохраняем никнейм (зашифрованный)
            if (!string.IsNullOrEmpty(authResponse?.User?.Nickname))
            {
                // Используем TokenManager для шифрования
                await _tokenManager.SetEncryptedAsync("nickname", authResponse.User.Nickname);
                _logger.LogInformation("✅ Saved nickname from server (encrypted): {Nickname}", authResponse.User.Nickname);
            }

            // Сохраняем ID пользователя (зашифрованный)
            if (authResponse?.User?.Id != Guid.Empty)
            {
                await _tokenManager.SetEncryptedAsync("user_id", authResponse.User.Id.ToString());
            }

            _logger.LogInformation("AuthService: Authentication successful for user {UserId}",
                authResponse?.User?.Id);

            return authResponse;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("AuthService: Authentication timeout");
            throw new TimeoutException("Authentication request timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AuthService: Network error during authentication");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuthService: Unexpected error during authentication");
            throw;
        }
        finally
        {
            _authLock.Release();
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string deviceId)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(refreshToken))
            throw new ArgumentNullException(nameof(refreshToken));

        if (string.IsNullOrEmpty(deviceId))
            throw new ArgumentNullException(nameof(deviceId));

        _logger.LogInformation("AuthService: Refreshing token for device {DeviceId}", deviceId);

        var request = new
        {
            RefreshToken = refreshToken,
            DeviceId = deviceId
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.PostAsync("api/auth/refresh", content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogError("AuthService: Refresh failed - {StatusCode} - {Error}",
                    response.StatusCode, error);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException($"Token refresh failed: {response.StatusCode}");
                }

                throw new HttpRequestException($"Token refresh failed: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cts.Token);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, options);

            if (authResponse == null)
            {
                throw new Exception("Failed to deserialize refresh response");
            }

            _logger.LogInformation("AuthService: Token refreshed successfully");

            return authResponse;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("AuthService: Refresh timeout");
            throw new TimeoutException("Refresh request timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuthService: Refresh failed");
            throw;
        }
    }

    public async Task<bool> CheckServerHealthAsync()
    {
        ThrowIfDisposed();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            _logger.LogDebug("AuthService: Checking server health");

            var liveResponse = await _httpClient.GetAsync("health/live", cts.Token);

            if (!liveResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Health check - live endpoint returned {StatusCode}",
                    liveResponse.StatusCode);
                return false;
            }

            var readyResponse = await _httpClient.GetAsync("health/ready", cts.Token);

            if (!readyResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Health check - ready endpoint returned {StatusCode}",
                    readyResponse.StatusCode);
                return false;
            }

            var content = await readyResponse.Content.ReadAsStringAsync(cts.Token);
            _logger.LogDebug("Health check details: {Content}", content);

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Health check - timeout");
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Health check - network error");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check - unexpected error");
            return false;
        }
    }

    public async Task<bool> WaitForServerAsync(int timeoutSeconds = 10, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var startTime = DateTime.Now;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _cts.Token,
            new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token);

        try
        {
            var attempt = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                attempt++;
                if (await CheckServerHealthAsync())
                {
                    _logger.LogInformation("✅ Server is healthy after {Attempt} attempts", attempt);
                    return true;
                }

                _logger.LogDebug("Health check attempt {Attempt} failed, retrying...", attempt);
                await Task.Delay(500, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("❌ Server health check timeout after {Timeout}s", timeoutSeconds);
        }

        return false;
    }

    #endregion

    #region Приватные методы

    private HttpClient ConfigureHttpClient(HttpClient httpClient)
    {
        // Для Android нужен специальный handler из-за сертификатов
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            httpClient = new HttpClient(handler);
        }

        httpClient.DefaultRequestHeaders.Accept.Add(new("application/json"));
        httpClient.BaseAddress = new Uri(_apiBaseUrl);

        // Добавляем User-Agent
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"HubblyMobile/{AppInfo.Current.VersionString} ({DeviceInfo.Current.Platform}; {DeviceInfo.Current.VersionString})");

        return httpClient;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AuthService));
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("AuthService: Disposing...");

        _cts.Cancel();
        _cts.Dispose();
        _authLock.Dispose();

        _httpClient?.CancelPendingRequests();
        _httpClient?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("AuthService: Disposed");
    }

    #endregion
}