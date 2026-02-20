namespace Hubbly.Mobile.Models;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserProfile User { get; set; } = new();
    public string DeviceId { get; set; } = string.Empty;
}
