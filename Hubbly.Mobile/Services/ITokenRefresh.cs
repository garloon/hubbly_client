namespace Hubbly.Mobile.Services;

public interface ITokenRefresh
{
    Task<string?> RefreshTokenAsync(AuthService authService);
    Task<string> GetValidTokenAsync(AuthService authService);
    Task<bool> HasValidTokenAsync();
    Task<TimeSpan?> GetTokenExpirationAsync(string key);
    Task<string> GetNicknameAsync();
}
