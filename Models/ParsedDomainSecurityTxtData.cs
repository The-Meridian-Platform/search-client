using System;
using System.Collections.Generic;

namespace MeridianClient.Models;

public class ParsedDomainSecurityTxtData
{
    public string Domain { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public IReadOnlyList<string> Contacts { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> SecurityEmails { get; set; } = Array.Empty<string>();
    public string PolicyUrl { get; set; } = string.Empty;
    public string AcknowledgmentsUrl { get; set; } = string.Empty;
    public string HiringUrl { get; set; } = string.Empty;
    public string Expires { get; set; } = string.Empty;

    public bool HasBugBounty => !string.IsNullOrWhiteSpace(PolicyUrl);
    public bool HasAcknowledgments => !string.IsNullOrWhiteSpace(AcknowledgmentsUrl);
    public bool HasHiring => !string.IsNullOrWhiteSpace(HiringUrl);
    public bool HasEmails => SecurityEmails != null && SecurityEmails.Count > 0;
    public bool HasExpires => !string.IsNullOrWhiteSpace(Expires);
}
