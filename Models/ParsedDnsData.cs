using System;

namespace MeridianClient.Models;

public class ParsedDnsData
{
    public string RecordType { get; set; } = "DNS";
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int? Ttl { get; set; }
    public int? Priority { get; set; }

    public string RecordTypeColor => (RecordType ?? string.Empty).ToUpperInvariant() switch
    {
        "A" => "#00D2FF",       // Electric Cyan
        "AAAA" => "#38BDF8",    // Sky
        "NS" => "#A855F7",      // Neon Purple
        "MX" => "#FFB800",      // Amber Gold
        "TXT" => "#00E599",     // Cyber Emerald
        "CNAME" => "#60A5FA",   // Blue
        "SOA" => "#F43F5E",     // Rose
        "PTR" => "#818CF8",     // Indigo
        _ => "#00D2FF"
    };

    public string RecordTypeBackground => (RecordType ?? string.Empty).ToUpperInvariant() switch
    {
        "A" => "#2000D2FF",
        "AAAA" => "#2038BDF8",
        "NS" => "#25A855F7",
        "MX" => "#25FFB800",
        "TXT" => "#2000E599",
        "CNAME" => "#2060A5FA",
        "SOA" => "#20F43F5E",
        "PTR" => "#20818CF8",
        _ => "#2000D2FF"
    };

    public bool HasTtl => Ttl.HasValue && Ttl.Value > 0;
    public bool HasPriority => Priority.HasValue && Priority.Value > 0;
}
