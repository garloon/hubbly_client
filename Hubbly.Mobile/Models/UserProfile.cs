namespace Hubbly.Mobile.Models;

public class UserProfile
{
    public Guid Id { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string AvatarConfigJson { get; set; } = "{}";
}
