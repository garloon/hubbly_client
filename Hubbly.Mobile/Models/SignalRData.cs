using System.Text.Json.Serialization;

namespace Hubbly.Mobile.Models;

// For UserJoined event
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

// For UserTyping event
public class UserTypingData
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;
}

// For UserLeft event
public class UserLeftData
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("leftAt")]
    public DateTimeOffset LeftAt { get; set; }
}
