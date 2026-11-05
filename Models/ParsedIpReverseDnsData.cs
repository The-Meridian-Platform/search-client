using System.Collections.Generic;
using System.Linq;

namespace MeridianClient.Models;

public sealed class ParsedIpReverseDnsData
{
    public string Ip { get; set; } = string.Empty;
    public string ArpaQuery { get; set; } = string.Empty;
    public IReadOnlyList<string> Hostnames { get; set; } = [];

    public bool HasHostnames => Hostnames.Count > 0;
    public bool HasArpaQuery => !string.IsNullOrWhiteSpace(ArpaQuery);
    public int HostnamesCount => Hostnames.Count;
    public string PrimaryHostname => Hostnames.FirstOrDefault() ?? string.Empty;
}
