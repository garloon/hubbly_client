using System.Text.Json.Serialization;

namespace Hubbly.Mobile.Models;

// Для события UserJoined
public class UserJoinedData
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("avatarConfigJson")]
    public string AvatarConfigJson { get; set; } = "{}";

    [JsonPropertyName("joinedAt")]
    public DateTimeOffset JoinedAt { get; set; }
}

// Для события UserTyping
public class UserTypingData
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;
}

// Для события UserLeft
public class UserLeftData
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("leftAt")]
    public DateTimeOffset LeftAt { get; set; }
}
