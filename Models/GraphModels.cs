using System;
using System.Collections.Generic;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MeridianClient.Models;

public enum GraphNodeType
{
    Target,
    Subdomain,
    IpAddress,
    NameServer,
    MailServer,
    Registrar,
    Contact,
    Technology,
    Profile,
    EmailVerified,
    Warning,
    Asn,
    Port,
    Location,
    Other
}

public partial class GraphNode : ObservableObject
{
    public string Id { get; }
    public string Label { get; }
    public GraphNodeType Type { get; }
    public string? RawJson { get; }

    [ObservableProperty]
    private string _subLabel = string.Empty;

    private readonly Dictionary<string, string> _properties;
    public IReadOnlyDictionary<string, string> Properties => _properties;

    public void SetProperty(string key, string value)
    {
        _properties[key] = value;
    }

    public void MergeProperties(IReadOnlyDictionary<string, string>? newProps)
    {
        if (newProps == null) return;
        foreach (var (k, v) in newProps)
        {
            if (!string.IsNullOrWhiteSpace(v))
            {
                _properties[k] = v;
            }
        }
    }

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isHighlighted;

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    private bool _isExpanding;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private int _expansionDepth;

    [ObservableProperty]
    private bool _isCollapsed;

    [ObservableProperty]
    private int _collapsedCount;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isThreat;

    [ObservableProperty]
    private string _threatDescription = string.Empty;

    public double Vx { get; set; }
    public double Vy { get; set; }
    public double Mass { get; set; } = 1.0;

    public double Radius => Type switch
    {
        GraphNodeType.Target => 28,
        GraphNodeType.Subdomain => 20,
        GraphNodeType.Profile => 21,
        GraphNodeType.EmailVerified => 19,
        GraphNodeType.Warning => 19,
        GraphNodeType.Asn => 17,
        GraphNodeType.Location => 17,
        GraphNodeType.Port => 15,
        GraphNodeType.IpAddress => 18,
        GraphNodeType.NameServer => 17,
        GraphNodeType.MailServer => 18,
        GraphNodeType.Technology => 16,
        GraphNodeType.Registrar => 16,
        GraphNodeType.Contact => 18,
        _ => 15
    };

    public string IconSymbol => Type switch
    {
        GraphNodeType.Target => "Target24",
        GraphNodeType.Subdomain => "Globe24",
        GraphNodeType.IpAddress => "Server24",
        GraphNodeType.NameServer => "Router24",
        GraphNodeType.MailServer => "Mail24",
        GraphNodeType.Technology => "Code24",
        GraphNodeType.Registrar => "Shield24",
        GraphNodeType.Contact => "PersonCircle24",
        GraphNodeType.Profile => "PersonCircle24",
        GraphNodeType.EmailVerified => "ShieldCheckmark24",
        GraphNodeType.Warning => "ShieldError24",
        GraphNodeType.Asn => "NetworkCheck24",
        GraphNodeType.Location => "Location24",
        GraphNodeType.Port => "PlugDisconnected24",
        _ => "DocumentBulletList24"
    };

    public string TypeNameRu => Type switch
    {
        GraphNodeType.Target => "Цель",
        GraphNodeType.Subdomain => "Поддомен",
        GraphNodeType.IpAddress => "IP-адрес",
        GraphNodeType.NameServer => "DNS сервер",
        GraphNodeType.MailServer => "Почтовый сервер",
        GraphNodeType.Technology => "Технология",
        GraphNodeType.Registrar => "Регистратор",
        GraphNodeType.Contact => "Контакт / Email",
        GraphNodeType.Profile => "OSINT Профиль",
        GraphNodeType.EmailVerified => "Легитимная почта",
        GraphNodeType.Warning => "Одноразовая почта",
        GraphNodeType.Asn => "AS Сеть",
        GraphNodeType.Location => "Геолокация",
        GraphNodeType.Port => "Открытый порт",
        _ => "Находка"
    };

    public string TypeName => Services.LocalizationService.Instance.CurrentLanguage == "en"
        ? Type switch
        {
            GraphNodeType.Target => "Target",
            GraphNodeType.Subdomain => "Subdomain",
            GraphNodeType.IpAddress => "IP Address",
            GraphNodeType.NameServer => "DNS Nameserver",
            GraphNodeType.MailServer => "Mail Server",
            GraphNodeType.Technology => "Web Technology",
            GraphNodeType.Registrar => "Registrar",
            GraphNodeType.Contact => "Contact / Email",
            GraphNodeType.Profile => "OSINT Profile",
            GraphNodeType.EmailVerified => "Legitimate Email",
            GraphNodeType.Warning => "Burner / Disposable Mail",
            GraphNodeType.Asn => "AS Network",
            GraphNodeType.Location => "Location",
            GraphNodeType.Port => "Open Port",
            _ => "Finding"
        }
        : TypeNameRu;

    public Color AccentColor => Type switch
    {
        GraphNodeType.Target => Color.FromRgb(6, 182, 212),       // Cyan #06B6D4
        GraphNodeType.Subdomain => Color.FromRgb(97, 175, 239),   // Blue #61AFEF
        GraphNodeType.IpAddress => Color.FromRgb(0, 229, 153),    // Neon Green #00E599
        GraphNodeType.NameServer => Color.FromRgb(168, 85, 247),  // Purple #A855F7
        GraphNodeType.MailServer => Color.FromRgb(245, 158, 11),  // Amber #F59E0B
        GraphNodeType.Technology => Color.FromRgb(20, 184, 166),  // Teal #14B8A6
        GraphNodeType.Registrar => Color.FromRgb(236, 72, 153),   // Pink #EC4899
        GraphNodeType.Contact => Color.FromRgb(234, 179, 8),      // Yellow #EAB308
        GraphNodeType.Profile => Color.FromRgb(137, 87, 229),     // Purple / GitHub #8957E5
        GraphNodeType.EmailVerified => Color.FromRgb(0, 229, 153),
        GraphNodeType.Warning => Color.FromRgb(255, 51, 102),
        GraphNodeType.Asn => Color.FromRgb(99, 102, 241),
        GraphNodeType.Location => Color.FromRgb(56, 189, 248),
        GraphNodeType.Port => Color.FromRgb(255, 184, 0),
        _ => Color.FromRgb(148, 163, 184)
    };

    public GraphNode(
        string id,
        string label,
        string subLabel,
        GraphNodeType type,
        string? rawJson = null,
        IReadOnlyDictionary<string, string>? properties = null)
    {
        Id = id;
        Label = label;
        _subLabel = subLabel;
        Type = type;
        RawJson = rawJson;
        _properties = properties != null ? new Dictionary<string, string>(properties) : new Dictionary<string, string>();
    }
}

public partial class GraphEdge : ObservableObject
{
    public string Id { get; }
    public GraphNode Source { get; }
    public GraphNode Target { get; }
    public string Label { get; }

    [ObservableProperty]
    private bool _isHighlighted;

    [ObservableProperty]
    private bool _isThreatPath;

    [ObservableProperty]
    private bool _isVisible = true;

    public GraphEdge(GraphNode source, GraphNode target, string label)
    {
        Source = source;
        Target = target;
        Label = label;
        Id = $"{source.Id}->{target.Id}:{label}";
    }
}
