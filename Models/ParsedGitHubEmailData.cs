namespace MeridianClient.Models;

public class ParsedGitHubEmailData
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ProfileUrl { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string AccountType { get; set; } = "User";

    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
    public bool HasProfileUrl => !string.IsNullOrWhiteSpace(ProfileUrl);
    public string FormattedUsername => !string.IsNullOrWhiteSpace(Username)
        ? (Username.StartsWith("@") ? Username : $"@{Username}")
        : "@unknown";
}
