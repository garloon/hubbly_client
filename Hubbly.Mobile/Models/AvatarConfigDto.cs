using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hubbly.Mobile.Models;

/// <summary>
/// Конфигурация аватара для клиента (DTO)
/// </summary>
public class AvatarConfigDto
{
    [JsonPropertyName("gender")]
    public string Gender { get; set; } = "male";

    [JsonPropertyName("baseModelId")]
    public string BaseModelId { get; set; } = "male_base";

    [JsonPropertyName("pose")]
    public string Pose { get; set; } = "standing";

    [JsonPropertyName("components")]
    public Dictionary<string, string> Components { get; set; } = new();

    // Статические методы
    public static AvatarConfigDto DefaultMale => new()
    {
        Gender = "male",
        BaseModelId = "male_base",
        Pose = "standing"
    };

    public static AvatarConfigDto DefaultFemale => new()
    {
        Gender = "female",
        BaseModelId = "female_base",
        Pose = "standing"
    };

    // Сериализация
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    public static AvatarConfigDto FromJson(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json) || json == "{}")
                return new AvatarConfigDto { Gender = "male" };

            return JsonSerializer.Deserialize<AvatarConfigDto>(json)
                   ?? new AvatarConfigDto { Gender = "male" };
        }
        catch
        {
            return new AvatarConfigDto { Gender = "male" };
        }
    }

    // Получить Emoji для превью
    public string GetEmoji() => Gender.ToLower() == "female" ? "👩" : "👨";
}
