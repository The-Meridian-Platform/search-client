using System;

namespace MeridianClient.Models;

public class ParsedDomainDmarcData
{
    public string Domain { get; set; } = string.Empty;
    public bool HasDmarc { get; set; }
    public string RawRecord { get; set; } = string.Empty;
    public string Policy { get; set; } = string.Empty;
    public bool IsSpoofable { get; set; }
    public string Severity { get; set; } = "Secure";
    public string ReportEmail { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;

    public bool HasReportEmail => !string.IsNullOrWhiteSpace(ReportEmail);

    public string PolicyBadgeText => (Policy?.ToLowerInvariant()) switch
    {
        "reject" => "STRICT REJECT",
        "quarantine" => "QUARANTINE",
        "none" => "MONITOR ONLY (NONE)",
        _ => IsSpoofable ? "NO POLICY / VULNERABLE" : "CONFIGURED"
    };

    public string SeverityColor => Severity?.ToLowerInvariant() switch
    {
        "secure" => "#00E599",
        "moderate" => "#FFB800",
        "warning" => "#FF3366",
        "critical" => "#FF3366",
        _ => IsSpoofable ? "#FF3366" : "#00E599"
    };

    public string SeverityBackground => Severity?.ToLowerInvariant() switch
    {
        "secure" => "#2000E599",
        "moderate" => "#20FFB800",
        "warning" => "#25FF3366",
        "critical" => "#25FF3366",
        _ => IsSpoofable ? "#25FF3366" : "#2000E599"
    };
}
