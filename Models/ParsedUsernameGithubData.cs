namespace MeridianClient.Models;

public sealed class ParsedUsernameGithubData
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string ProfileUrl { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string? Email { get; set; }
    public string? Blog { get; set; }
    public string? Twitter { get; set; }
    public int PublicRepos { get; set; }
    public int Followers { get; set; }

    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
    public bool HasProfileUrl => !string.IsNullOrWhiteSpace(ProfileUrl);
    public bool HasName => !string.IsNullOrWhiteSpace(Name);
    public bool HasBio => !string.IsNullOrWhiteSpace(Bio);
    public bool HasCompany => !string.IsNullOrWhiteSpace(Company);
    public bool HasLocation => !string.IsNullOrWhiteSpace(Location);
    public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
    public bool HasBlog => !string.IsNullOrWhiteSpace(Blog);
    public bool HasTwitter => !string.IsNullOrWhiteSpace(Twitter);

    public string FormattedUsername => !string.IsNullOrWhiteSpace(Username)
        ? (Username.StartsWith("@") ? Username : $"@{Username}")
        : "@unknown";

    public string DisplayTitle => !string.IsNullOrWhiteSpace(Name)
        ? Name
        : (!string.IsNullOrWhiteSpace(Username) ? Username : "GitHub User");

    public string FormattedFollowers => Followers >= 1000
        ? $"{(Followers / 1000.0):0.#}k"
        : Followers.ToString();

    public string FormattedRepos => PublicRepos.ToString();
}
