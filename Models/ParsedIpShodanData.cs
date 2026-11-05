using System.Collections.Generic;

namespace MeridianClient.Models;

public sealed class ParsedIpShodanData
{
    public string Ip { get; set; } = string.Empty;
    public IReadOnlyList<int> Ports { get; set; } = [];
    public IReadOnlyList<string> Hostnames { get; set; } = [];
    public IReadOnlyList<string> Tags { get; set; } = [];
    public IReadOnlyList<string> Cpes { get; set; } = [];
    public IReadOnlyList<string> Vulnerabilities { get; set; } = [];

    public bool HasVulns => Vulnerabilities.Count > 0;
    public bool HasPorts => Ports.Count > 0;
    public bool HasHostnames => Hostnames.Count > 0;
    public bool HasTags => Tags.Count > 0;
    public bool HasCpes => Cpes.Count > 0;
    public int PortsCount => Ports.Count;
    public int VulnsCount => Vulnerabilities.Count;
    public string FormattedPortsSummary => HasPorts ? string.Join(", ", Ports) : "Нет портов";
}
