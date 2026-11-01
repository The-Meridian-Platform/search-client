using System.Text.Json.Serialization;

namespace MeridianClient.Models;

public class FindingModel
{
    [JsonPropertyName("findingId")]
    public string? FindingId { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("providerKey")]
    public string ProviderKey { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("dataJson")]
    public string DataJson { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonIgnore]
    public string EffectiveId => !string.IsNullOrWhiteSpace(FindingId) ? FindingId : (Id ?? string.Empty);
}
