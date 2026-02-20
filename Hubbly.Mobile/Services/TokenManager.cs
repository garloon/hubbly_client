using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Hubbly.Mobile.Services;

public class TokenManager : IDisposable
{
    private readonly ILogger<TokenManager> _logger;
    private readonly ConcurrentDictionary<string, TokenInfo> _tokenCache = new();
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly SemaphoreSlim _storageLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private readonly EncryptionService _encryption;

    private bool _isRefreshing;
    private Task<string?> _currentRefreshTask;
    private bool _disposed;

    public TokenManager(ILogger<TokenManager> logger, EncryptionService encryption)
    {
        _logger = logger;
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));

        // Load tokens from storage on startup
        Task.Run(async () => await LoadTokensFromStorageAsync());
    }

    #region Public Methods

    public async Task SetAsync(string key, string value, TimeSpan? expiresIn = null)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        _logger.LogDebug($"📝 TokenManager.Set: {key} = {value?.Substring(0, Math.Min(8, value?.Length ?? 0))}...");

        var tokenInfo = new TokenInfo
        {
            Value = value,
            ExpiresAt = expiresIn.HasValue
                ? DateTimeOffset.UtcNow.Add(expiresIn.Value)
                : null
        };

        // Store in cache
        _tokenCache[key] = tokenInfo;

        // Save to persistent storage in background
        _ = Task.Run(async () => await SaveToStorageAsync(key, tokenInfo));
    }

    public async Task<string> GetAsync(string key)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var tokenInfo = await GetTokenInfoAsync(key);
        var valuePreview = tokenInfo?.Value?.Substring(0, Math.Min(8, tokenInfo?.Value?.Length ?? 0));
        _logger.LogDebug($"📖 TokenManager.Get: {key} = {valuePreview}...");

        return tokenInfo?.Value ?? string.Empty;
    }

    public async Task<string?> GetValidTokenAsync(AuthService authService)
    {
        ThrowIfDisposed();

        if (authService == null)
            throw new ArgumentNullException(nameof(authService));

        // 1. Check current token
        var tokenInfo = await GetTokenInfoAsync("access_token");
        if (tokenInfo != null && !tokenInfo.IsExpired)
        {
            _logger.LogDebug("TokenManager: Valid token found");
            return tokenInfo.Value;
        }

        // 2. Use "async lazy initialization" pattern
        if (!await _refreshLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            _logger.LogError("TokenManager: Failed to acquire refresh lock within 5 seconds");
            return null;
        }

        try
        {
            // Double-check after acquiring lock
            tokenInfo = await GetTokenInfoAsync("access_token");
            if (tokenInfo != null && !tokenInfo.IsExpired)
            {
                return tokenInfo.Value;
            }

            // If already refreshing - join existing task
            if (_isRefreshing && _currentRefreshTask != null)
            {
                _logger.LogInformation("TokenManager: Refresh already in progress, waiting...");
                return await _currentRefreshTask;
            }

            // Start refresh
            _isRefreshing = true;
            _currentRefreshTask = RefreshTokenInternalAsync(authService);

            return await _currentRefreshTask;
        }
        finally
        {
            _isRefreshing = false;
            _currentRefreshTask = null;
            _refreshLock.Release();
        }
    }

    public void Clear()
    {
        ThrowIfDisposed();

        _logger.LogInformation("TokenManager: Clearing all tokens");

        lock (_storageLock)
        {
            _tokenCache.Clear();

            // Clear SecureStorage
            try
            {
                SecureStorage.RemoveAll();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear SecureStorage");
            }

            // Clear Preferences
            try
            {
                Preferences.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear Preferences");
            }
        }
    }

    public async Task<bool> HasValidTokenAsync()
    {
        ThrowIfDisposed();

        var tokenInfo = await GetTokenInfoAsync("access_token");
        return tokenInfo != null && !tokenInfo.IsExpired;
    }

    public async Task<TimeSpan?> GetTokenExpirationAsync(string key)
    {
        ThrowIfDisposed();

        var tokenInfo = await GetTokenInfoAsync(key);
        if (tokenInfo?.ExpiresAt == null)
            return null;

        var remaining = tokenInfo.ExpiresAt.Value - DateTimeOffset.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public async Task<string> GetNicknameAsync()
    {
        // Check cache first
        var cached = await GetAsync("nickname");
        if (!string.IsNullOrEmpty(cached))
            return cached;

        // Try Preferences as fallback (should be encrypted)
        var pref = await GetEncryptedAsync("nickname");
        if (!string.IsNullOrEmpty(pref))
        {
            await SetEncryptedAsync("nickname", pref);
            return pref;
        }

        return "Guest";
    }

    #endregion

    #region Encrypted Storage for Sensitive Data (Not Just Tokens)

    public Task SetEncryptedAsync(string key, string value)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        _logger.LogDebug($"🔒 TokenManager.SetEncrypted: {key} = {value?.Substring(0, Math.Min(8, value?.Length ?? 0))}...");

        // Encrypt and store in Preferences only (no SecureStorage duplicate for non-token data)
        var encryptedValue = _encryption.Encrypt(value);
        Preferences.Set(key, encryptedValue);

        // Also cache in memory for quick access
        _tokenCache[key] = new TokenInfo { Value = value };

        return Task.CompletedTask;
    }

    public async Task<string?> GetEncryptedAsync(string key)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        // Check cache first
        if (_tokenCache.TryGetValue(key, out var cached))
        {
            _logger.LogDebug($"🔓 TokenManager.GetEncrypted (cached): {key}");
            return cached.Value;
        }

        // Try to read from Preferences and decrypt
        try
        {
            var encryptedValue = Preferences.Get(key, string.Empty);
            if (!string.IsNullOrEmpty(encryptedValue))
            {
                var decryptedValue = _encryption.Decrypt(encryptedValue);
                _logger.LogDebug($"🔓 TokenManager.GetEncrypted (decrypted): {key}");

                // Cache for future
                _tokenCache[key] = new TokenInfo { Value = decryptedValue };
                return decryptedValue;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt {Key} from Preferences", key);
        }

        return null;
    }

    #endregion

    #region Private Methods

    private async Task<TokenInfo?> GetTokenInfoAsync(string key)
    {
        // Check cache
        if (_tokenCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // Try to load from storage
        return await LoadFromStorageAsync(key);
    }

    private async Task<TokenInfo?> LoadFromStorageAsync(string key)
    {
        try
        {
            // Try SecureStorage
            var value = await SecureStorage.GetAsync(key);
            var expiresStr = await SecureStorage.GetAsync($"{key}_expires");

            if (!string.IsNullOrEmpty(value))
            {
                var tokenInfo = new TokenInfo { Value = value };

                if (!string.IsNullOrEmpty(expiresStr) &&
                    DateTimeOffset.TryParse(expiresStr, out var expires))
                {
                    tokenInfo.ExpiresAt = expires;
                }

                // Store in cache
                _tokenCache[key] = tokenInfo;

                return tokenInfo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load {Key} from SecureStorage", key);
        }

        // Fallback to Preferences (with decryption)
        try
        {
            var prefValue = Preferences.Get(key, string.Empty);
            var prefExpires = Preferences.Get($"{key}_expires", string.Empty);

            if (!string.IsNullOrEmpty(prefValue))
            {
                // Decrypt value
                var decryptedValue = _encryption.Decrypt(prefValue);
                var tokenInfo = new TokenInfo { Value = decryptedValue };

                if (!string.IsNullOrEmpty(prefExpires) &&
                    DateTimeOffset.TryParse(prefExpires, out var expires))
                {
                    tokenInfo.ExpiresAt = expires;
                }

                // Store in cache
                _tokenCache[key] = tokenInfo;

                return tokenInfo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load {Key} from Preferences", key);
        }

        return null;
    }

    private async Task LoadTokensFromStorageAsync()
    {
        try
        {
            var keys = new[] { "access_token", "refresh_token", "user_id" };

            foreach (var key in keys)
            {
                await LoadFromStorageAsync(key);
            }

            _logger.LogInformation("TokenManager: Loaded tokens from storage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tokens from storage");
        }
    }

    private async Task SaveToStorageAsync(string key, TokenInfo tokenInfo)
    {
        if (!await _storageLock.WaitAsync(TimeSpan.FromSeconds(2)))
        {
            _logger.LogWarning("Failed to acquire storage lock for {Key}", key);
            return;
        }

        try
        {
            // Save to SecureStorage (already encrypted by OS)
            await SecureStorage.SetAsync(key, tokenInfo.Value);

            if (tokenInfo.ExpiresAt.HasValue)
            {
                await SecureStorage.SetAsync($"{key}_expires", tokenInfo.ExpiresAt.Value.ToString("o"));
            }

            // Duplicate in Preferences with additional encryption
            var encryptedValue = _encryption.Encrypt(tokenInfo.Value);
            Preferences.Set(key, encryptedValue);

            if (tokenInfo.ExpiresAt.HasValue)
            {
                // Expiration doesn't need encryption as it's not sensitive
                Preferences.Set($"{key}_expires", tokenInfo.ExpiresAt.Value.ToString("o"));
            }

            _logger.LogDebug("Saved {Key} to storage (encrypted in Preferences)", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save {Key} to storage", key);
        }
        finally
        {
            _storageLock.Release();
        }
    }

    private async Task<string?> RefreshTokenInternalAsync(AuthService authService)
    {
        try
        {
            _logger.LogInformation("TokenManager: Starting token refresh...");

            var refreshToken = await GetAsync("refresh_token");
            var deviceId = Preferences.Get("server_device_id",
                Preferences.Get("persistent_device_id", ""));

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("TokenManager: Missing refresh token");
                return null;
            }

            if (string.IsNullOrEmpty(deviceId))
            {
                _logger.LogWarning("TokenManager: Missing device ID");
                return null;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var authResponse = await authService.RefreshTokenAsync(refreshToken, deviceId);

            // Save new tokens
            await SetAsync("access_token", authResponse.AccessToken, TimeSpan.FromMinutes(15));
            await SetAsync("refresh_token", authResponse.RefreshToken, TimeSpan.FromDays(7));

            if (!string.IsNullOrEmpty(authResponse.DeviceId))
            {
                Preferences.Set("server_device_id", authResponse.DeviceId);
            }

            _logger.LogInformation("TokenManager: Token refreshed successfully");
            return authResponse.AccessToken;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("TokenManager: Refresh cancelled");
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "TokenManager: Refresh failed - unauthorized");

            // On critical auth error - clear everything
            Clear();

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TokenManager: Refresh failed");
            return null;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TokenManager));
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("TokenManager: Disposing...");

        _cts.Cancel();
        _cts.Dispose();

        _refreshLock?.Dispose();
        _storageLock?.Dispose();

        _tokenCache.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("TokenManager: Disposed");
    }

    #endregion

    #region Internal Classes

    private class TokenInfo
    {
        public string Value { get; set; } = string.Empty;
        public DateTimeOffset? ExpiresAt { get; set; }

        public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow >= ExpiresAt.Value;
    }

    #endregion
}
