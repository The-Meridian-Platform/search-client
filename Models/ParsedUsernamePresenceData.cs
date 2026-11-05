namespace MeridianClient.Models;

public sealed class ParsedUsernamePresenceData
{
    public string Username { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public string? Details { get; set; }

    public bool HasUrl => !string.IsNullOrWhiteSpace(Url);
    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

    public string FormattedUsername => !string.IsNullOrWhiteSpace(Username)
        ? (Username.StartsWith("@") ? Username : $"@{Username}")
        : "@unknown";

    public string PlatformBadgeColor => Platform.ToLowerInvariant() switch
    {
        "reddit" => "#FF4500",
        "steam" => "#1b2838",
        "gitlab" => "#FC6D26",
        "hackernews" => "#FF6600",
        _ => "#00D2FF"
    };

    public string PlatformBadgeForeground => Platform.ToLowerInvariant() switch
    {
        "reddit" => "#FF5722",
        "steam" => "#66C0F4",
        "gitlab" => "#FC6D26",
        "hackernews" => "#FF7700",
        _ => "#00D2FF"
    };

    public string PlatformBadgeBackground => Platform.ToLowerInvariant() switch
    {
        "reddit" => "#25FF4500",
        "steam" => "#2566C0F4",
        "gitlab" => "#25FC6D26",
        "hackernews" => "#25FF6600",
        _ => "#2000D2FF"
    };
}
