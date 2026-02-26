namespace Hubbly.Mobile.Services;

public interface ITokenRefresh
{
    Task<string?> RefreshTokenAsync();
    Task<string> GetValidTokenAsync();
    Task<bool> HasValidTokenAsync();
    Task<TimeSpan?> GetTokenExpirationAsync(string key);
    Task<string> GetNicknameAsync();
}
