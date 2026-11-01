namespace MeridianClient.Models;

public class ParsedGravatarEmailData
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AboutMe { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ProfileUrl { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
    public bool HasProfileUrl => !string.IsNullOrWhiteSpace(ProfileUrl);
    public bool HasAboutMe => !string.IsNullOrWhiteSpace(AboutMe);
    public bool HasLocation => !string.IsNullOrWhiteSpace(Location);
    public string EffectiveName => !string.IsNullOrWhiteSpace(DisplayName)
        ? DisplayName
        : (!string.IsNullOrWhiteSpace(Username) ? Username : "Gravatar User");
    public string FormattedUsername => !string.IsNullOrWhiteSpace(Username)
        ? (Username.StartsWith("@") ? Username : $"@{Username}")
        : string.Empty;
}
