using System.Globalization;
using System.Text.Json.Serialization;

namespace MeridianClient.Models;

public class SearchSessionModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("searchSessionId")]
    public string? SearchSessionId { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonIgnore]
    public string EffectiveId => !string.IsNullOrWhiteSpace(Id)
        ? Id
        : !string.IsNullOrWhiteSpace(SearchSessionId)
            ? SearchSessionId
            : SessionId ?? string.Empty;

    [JsonPropertyName("target")]
    public SearchTarget? Target { get; set; }

    [JsonPropertyName("targetValue")]
    public string? TargetValue { get; set; }

    [JsonIgnore]
    public string EffectiveTarget => Target?.Value ?? TargetValue ?? string.Empty;

    [JsonPropertyName("status")]
    public object? StatusRaw { get; set; }

    [JsonIgnore]
    public string Status => StatusRaw?.ToString() ?? "Pending";

    [JsonPropertyName("startedAt")]
    public string? StartedAt { get; set; }

    [JsonPropertyName("finishedAt")]
    public string? FinishedAt { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonIgnore]
    public string FormattedCreatedAt => FormatIsoTimestamp(CreatedAt);

    [JsonIgnore]
    public string FormattedFinishedAt => FormatIsoTimestamp(FinishedAt);

    [JsonIgnore]
    public string FormattedDuration => GetDurationText(StartedAt ?? CreatedAt, FinishedAt);

    [JsonIgnore]
    public string FormattedTitle => !string.IsNullOrEmpty(EffectiveTarget)
        ? $"{EffectiveTarget} ({Status}) [{EffectiveId[..Math.Min(6, EffectiveId.Length)]}]"
        : $"Сессия [{EffectiveId[..Math.Min(6, EffectiveId.Length)]}]";

    public static string FormatIsoTimestamp(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "—";
        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
        {
            return dto.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss.fff");
        }
        return raw;
    }

    public static string GetDurationText(string? startRaw, string? endRaw)
    {
        if (string.IsNullOrWhiteSpace(startRaw) || string.IsNullOrWhiteSpace(endRaw)) return string.Empty;
        if (DateTimeOffset.TryParse(startRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) &&
            DateTimeOffset.TryParse(endRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            var diff = end - start;
            if (diff.TotalMilliseconds < 0) return string.Empty;
            return $"{diff.TotalSeconds:F3}s";
        }
        return string.Empty;
    }

    private int _totalFindings;

    [JsonPropertyName("totalFindings")]
    public int TotalFindings
    {
        get => _totalFindings > 0 ? _totalFindings : (Finding?.Count ?? FindingsList?.Count ?? 0);
        set => _totalFindings = value;
    }

    [JsonPropertyName("finding")]
    public List<FindingModel>? Finding { get; set; }

    [JsonPropertyName("findings")]
    public List<FindingModel>? FindingsList { get; set; }

    [JsonIgnore]
    public List<FindingModel> Findings => Finding ?? FindingsList ?? new List<FindingModel>();
}
