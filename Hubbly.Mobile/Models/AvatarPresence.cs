namespace Hubbly.Mobile.Models;

public class AvatarPresence
{
    public Guid UserId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public AvatarConfigDto AvatarConfig { get; set; } = null!;
    public DateTimeOffset JoinedAt { get; set; }
    public bool IsCurrentUser { get; set; }
}
