using System;
using System.Collections.Generic;

namespace MeridianClient.Models;

public class ParsedWhoisData
{
    public string DomainName { get; set; } = string.Empty;
    public string Registrar { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
    public string ExpiresDate { get; set; } = string.Empty;
    public string UpdatedDate { get; set; } = string.Empty;
    public List<string> NameServers { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public string RegistrantCountry { get; set; } = string.Empty;
    public string RegistrantOrg { get; set; } = string.Empty;

    public bool HasRegistrar => !string.IsNullOrWhiteSpace(Registrar);
    public bool HasCreatedDate => !string.IsNullOrWhiteSpace(CreatedDate);
    public bool HasExpiresDate => !string.IsNullOrWhiteSpace(ExpiresDate);
    public bool HasCountry => !string.IsNullOrWhiteSpace(RegistrantCountry);
    public bool HasStatus => !string.IsNullOrWhiteSpace(Status);
    public bool HasNameServers => NameServers != null && NameServers.Count > 0;
}
