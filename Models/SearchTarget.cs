using System.Text.Json.Serialization;

namespace MeridianClient.Models;

public class SearchTarget
{
    [JsonPropertyName("targetType")]
    public string TargetType { get; set; } = "Domain";

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
