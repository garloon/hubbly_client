namespace Hubbly.Mobile.Models;

public class RoomAssignmentData
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int UsersInRoom { get; set; }
    public int MaxUsers { get; set; }
}
