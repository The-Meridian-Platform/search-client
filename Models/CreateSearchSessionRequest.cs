using System.Text.Json.Serialization;

namespace MeridianClient.Models;

public class CreateSearchSessionRequest
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("targetType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SearchTargetType? TargetType { get; set; }

    [JsonPropertyName("target")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SearchTarget? Target { get; set; }
}

public class CreateSearchSessionResponse
{
    [JsonPropertyName("searchSessionId")]
    public string? SearchSessionId { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonIgnore]
    public string EffectiveId => !string.IsNullOrWhiteSpace(SearchSessionId)
        ? SearchSessionId
        : !string.IsNullOrWhiteSpace(SessionId)
            ? SessionId
            : Id ?? string.Empty;
}
