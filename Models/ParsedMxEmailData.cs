using System.Collections.Generic;
using MeridianClient.Services;

namespace MeridianClient.Models;

public class ParsedMxEmailData
{
    public string EmailDomain { get; set; } = string.Empty;
    public string MailProviderName { get; set; } = string.Empty;
    public List<string> MxServers { get; set; } = new();
    public bool IsEnterprise { get; set; }
    public bool HasSpfProtection { get; set; }

    public bool HasMxServers => MxServers.Count > 0;
    public string FormattedMxServers => string.Join(", ", MxServers);
    public string SpfBadgeText => HasSpfProtection
        ? LocalizationService.Instance.GetString("Search_Card_Mx_SpfActive")
        : LocalizationService.Instance.GetString("Search_Card_Mx_SpfMissing");
    public string SpfBadgeColor => HasSpfProtection ? "#00E599" : "#F59E0B";
    public string SpfBadgeBackground => HasSpfProtection ? "#2000E599" : "#25F59E0B";
}
