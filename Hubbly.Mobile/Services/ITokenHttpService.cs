using Hubbly.Mobile.Models;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Services;

public interface ITokenHttpService
{
    Task<AuthResponse> AuthenticateGuestWithAvatarAsync(string avatarConfigJson);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, string deviceId);
    Task<bool> CheckServerHealthAsync();
    Task<bool> WaitForServerAsync(int timeoutSeconds = 10, CancellationToken cancellationToken = default);
    Task<bool> ConvertGuestToUserAsync(Guid guestUserId);
}
