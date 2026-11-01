using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MeridianClient.Models;

public class InvestigationModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("rootTarget")]
    public string RootTarget { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("sessionIds")]
    public List<string> SessionIds { get; set; } = new();

    [JsonPropertyName("cachedFindings")]
    public List<FindingModel> CachedFindings { get; set; } = new();

    [JsonIgnore]
    public string FormattedTitle => !string.IsNullOrWhiteSpace(Title)
        ? $"{Title} ({Pluralize(SessionIds.Count, "сессия", "сессии", "сессий")}, {Pluralize(CachedFindings.Count, "находка", "находки", "находок")})"
        : $"Расследование [{Id[..Math.Min(6, Id.Length)]}]";

    private static string Pluralize(int number, string one, string two, string five)
    {
        int n = Math.Abs(number) % 100;
        int n1 = n % 10;
        if (n > 10 && n < 20) return $"{number} {five}";
        if (n1 > 1 && n1 < 5) return $"{number} {two}";
        if (n1 == 1) return $"{number} {one}";
        return $"{number} {five}";
    }

    [JsonIgnore]
    public string FormattedDate => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}
