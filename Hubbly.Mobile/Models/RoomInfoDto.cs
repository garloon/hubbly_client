using System.Text.Json.Serialization;

namespace Hubbly.Mobile.Models;

public partial class RoomInfoDto
{
    [JsonPropertyName("roomId")]
    public Guid RoomId { get; set; }

    [JsonPropertyName("roomName")]
    public string RoomName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonIgnore]
    public RoomType RoomType => (RoomType)Type;

    [JsonPropertyName("currentUsers")]
    public int CurrentUsers { get; set; }

    [JsonPropertyName("maxUsers")]
    public int MaxUsers { get; set; }

    [JsonPropertyName("isPrivate")]
    public bool IsPrivate { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("lastActiveAt")]
    public DateTimeOffset LastActiveAt { get; set; }
}