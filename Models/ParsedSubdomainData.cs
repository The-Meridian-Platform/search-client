namespace MeridianClient.Models;

public class ParsedSubdomainData
{
    public string Subdomain { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string ValidFrom { get; set; } = string.Empty;
    public string ValidTo { get; set; } = string.Empty;

    public bool HasIssuer => !string.IsNullOrWhiteSpace(Issuer);
    public bool HasValidity => !string.IsNullOrWhiteSpace(ValidFrom) || !string.IsNullOrWhiteSpace(ValidTo);
}
