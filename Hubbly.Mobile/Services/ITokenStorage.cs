namespace Hubbly.Mobile.Services;

public interface ITokenStorage
{
    Task SetAsync(string key, string value, TimeSpan? expiresIn = null);
    Task<string?> GetAsync(string key);
    Task<bool> HasAsync(string key);
    Task RemoveAsync(string key);
    Task ClearAsync();
}
