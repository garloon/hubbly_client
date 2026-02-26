using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Services;

public class TokenStorage : ITokenStorage
{
    private readonly ILogger<TokenStorage> _logger;
    private readonly ConcurrentDictionary<string, TokenInfo> _cache = new();

    public TokenStorage(ILogger<TokenStorage> logger)
    {
        _logger = logger;
    }

    public async Task SetAsync(string key, string value, TimeSpan? expiresIn = null)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        _logger.LogDebug($"💾 TokenStorage.Set: {key} (value hidden for security)");

        var tokenInfo = new TokenInfo
        {
            Value = value,
            ExpiresAt = expiresIn.HasValue
                ? DateTimeOffset.UtcNow.Add(expiresIn.Value)
                : null
        };

        // Store in cache
        _cache[key] = tokenInfo;

        // Save to persistent storage synchronously (no fire-and-forget)
        await SaveToPreferencesAsync(key, tokenInfo);
    }

    public async Task<string?> GetAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        // Check cache first
        if (_cache.TryGetValue(key, out var cached))
        {
            _logger.LogDebug($"📖 TokenStorage.Get (cached): {key}");
            return cached.Value;
        }

        // Try to load from storage
        var tokenInfo = await LoadFromPreferencesAsync(key);
        if (tokenInfo != null)
        {
            _cache[key] = tokenInfo;
            return tokenInfo.Value;
        }

        return null;
    }

    public async Task<bool> HasAsync(string key)
    {
        var value = await GetAsync(key);
        return !string.IsNullOrEmpty(value);
    }

    public async Task RemoveAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        _cache.TryRemove(key, out _);

        try
        {
            Preferences.Remove(key);
            Preferences.Remove($"{key}_expires");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove {Key} from Preferences", key);
        }
    }

    public async Task ClearAsync()
    {
        _cache.Clear();

        try
        {
            var keys = new[] { "access_token", "refresh_token", "user_id", "persistent_device_id", "server_device_id", "nickname" };
            foreach (var key in keys)
            {
                Preferences.Remove(key);
                Preferences.Remove($"{key}_expires");
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear Preferences");
        }
    }

    private async Task SaveToPreferencesAsync(string key, TokenInfo tokenInfo)
    {
        try
        {
            Preferences.Set(key, tokenInfo.Value);

            if (tokenInfo.ExpiresAt.HasValue)
            {
                Preferences.Set($"{key}_expires", tokenInfo.ExpiresAt.Value.ToString("o"));
            }

            _logger.LogDebug("Saved {Key} to Preferences", key);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save {Key} to Preferences", key);
        }
    }

    private async Task<TokenInfo?> LoadFromPreferencesAsync(string key)
    {
        try
        {
            var value = Preferences.Get(key, string.Empty);
            if (!string.IsNullOrEmpty(value))
            {
                var tokenInfo = new TokenInfo { Value = value };

                var expiresStr = Preferences.Get($"{key}_expires", string.Empty);
                if (!string.IsNullOrEmpty(expiresStr) && DateTimeOffset.TryParse(expiresStr, out var expires))
                {
                    tokenInfo.ExpiresAt = expires;
                }

                _logger.LogDebug($"📖 TokenStorage.Get (from Preferences): {key}");
                return tokenInfo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read {Key} from Preferences", key);
        }

        return null;
    }
}
