namespace MeridianClient.Models;

public sealed class ParsedUsernameTelegramData
{
    public string Username { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public string Url { get; set; } = string.Empty;

    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool HasUrl => !string.IsNullOrWhiteSpace(Url);

    public string FormattedUsername => !string.IsNullOrWhiteSpace(Username)
        ? (Username.StartsWith("@") ? Username : $"@{Username}")
        : "@unknown";

    public string DisplayTitle => !string.IsNullOrWhiteSpace(Title)
        ? Title
        : (!string.IsNullOrWhiteSpace(Username) ? Username : "Telegram Profile");
}
