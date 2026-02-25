using System.Text.Json.Serialization;

namespace Hubbly.Mobile.Models;

/// <summary>
/// Partial class to add UI-specific properties to RoomInfoDto
/// </summary>
public partial class RoomInfoDto
{
    /// <summary>
    /// Indicates if this room is the current room for the user
    /// </summary>
    [JsonIgnore]
    public bool IsCurrent { get; set; }
    
    /// <summary>
    /// Text to display on the join/current button for this room
    /// </summary>
    [JsonIgnore]
    public string ButtonText { get; set; } = "Join";
}