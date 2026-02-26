using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Services;

public class TokenManager : IDisposable
{
    private readonly ILogger<TokenManager> _logger;
    private readonly ITokenStorage _storage;
    private readonly ITokenRefresh _refresh;
    private bool _disposed;

    public TokenManager(ITokenStorage storage, ITokenRefresh refresh, ILogger<TokenManager> logger)
    {
        _storage = storage;
        _refresh = refresh;
        _logger = logger;
    }

    #region Public Methods (Facade)

    public async Task SetAsync(string key, string value, TimeSpan? expiresIn = null)
    {
        ThrowIfDisposed();
        await _storage.SetAsync(key, value, expiresIn);
    }

    public async Task<string> GetAsync(string key)
    {
        ThrowIfDisposed();
        var value = await _storage.GetAsync(key);
        _logger.LogDebug($"📖 TokenManager.Get: {key} (value hidden for security)");
        return value ?? string.Empty;
    }

    public async Task<string?> GetValidTokenAsync(AuthService authService)
    {
        ThrowIfDisposed();
        return await _refresh.GetValidTokenAsync(authService);
    }

    public void Clear()
    {
        ThrowIfDisposed();
        _logger.LogInformation("TokenManager: Clearing all tokens");
        _storage.ClearAsync().GetAwaiter().GetResult();
    }

    public async Task<bool> HasValidTokenAsync()
    {
        ThrowIfDisposed();
        return await _refresh.HasValidTokenAsync();
    }

    public async Task<TimeSpan?> GetTokenExpirationAsync(string key)
    {
        ThrowIfDisposed();
        return await _refresh.GetTokenExpirationAsync(key);
    }

    public async Task<string> GetNicknameAsync()
    {
        ThrowIfDisposed();
        return await _refresh.GetNicknameAsync();
    }

    public async Task<string?> GetEncryptedAsync(string key)
    {
        ThrowIfDisposed();
        // For now, just use regular storage (no encryption)
        // This maintains compatibility with the existing code
        return await _storage.GetAsync(key);
    }

    public async Task SetEncryptedAsync(string key, string value)
    {
        ThrowIfDisposed();
        // For now, just use regular storage (no encryption)
        // This maintains compatibility with the existing code
        await _storage.SetAsync(key, value);
    }

    #endregion

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TokenManager));
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("TokenManager: Disposing...");

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("TokenManager: Disposed");
    }
}
