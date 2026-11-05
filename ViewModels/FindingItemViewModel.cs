using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeridianClient.Models;
using MeridianClient.Services;
using MeridianClient.Views;

namespace MeridianClient.ViewModels;

public partial class FindingItemViewModel : ObservableObject
{
    public FindingModel Model { get; }

    public string FindingId => Model.EffectiveId;
    public string ProviderKey => Model.ProviderKey;
    public string Type => Model.Type;
    public string CreatedAt => SearchSessionModel.FormatIsoTimestamp(Model.CreatedAt);
    public string RawJson => Model.DataJson;

    [ObservableProperty]
    private string _formattedJson = string.Empty;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isDns;

    [ObservableProperty]
    private bool _isWhois;

    [ObservableProperty]
    private bool _isSubdomain;

    [ObservableProperty]
    private bool _isHttpTech;

    [ObservableProperty]
    private bool _isGitHubEmail;

    [ObservableProperty]
    private bool _isGravatarEmail;

    [ObservableProperty]
    private bool _isDisposableEmail;

    [ObservableProperty]
    private bool _isMxEmail;

    [ObservableProperty]
    private bool _isIpGeo;

    [ObservableProperty]
    private bool _isIpReverseDns;

    [ObservableProperty]
    private bool _isIpShodan;

    [ObservableProperty]
    private bool _isUsernameGithub;

    [ObservableProperty]
    private bool _isUsernameTelegram;

    [ObservableProperty]
    private bool _isUsernamePresence;

    [ObservableProperty]
    private ParsedDnsData? _dnsData;

    [ObservableProperty]
    private ParsedWhoisData? _whoisData;

    [ObservableProperty]
    private ParsedSubdomainData? _subdomainData;

    [ObservableProperty]
    private ParsedHttpTechData? _httpTechData;

    [ObservableProperty]
    private ParsedGitHubEmailData? _gitHubEmailData;

    [ObservableProperty]
    private ParsedGravatarEmailData? _gravatarEmailData;

    [ObservableProperty]
    private ParsedDisposableEmailData? _disposableEmailData;

    [ObservableProperty]
    private ParsedMxEmailData? _mxEmailData;

    [ObservableProperty]
    private ParsedIpGeoData? _ipGeoData;

    [ObservableProperty]
    private ParsedIpReverseDnsData? _ipReverseDnsData;

    [ObservableProperty]
    private ParsedIpShodanData? _ipShodanData;

    [ObservableProperty]
    private ParsedUsernameGithubData? _usernameGithubData;

    [ObservableProperty]
    private ParsedUsernameTelegramData? _usernameTelegramData;

    [ObservableProperty]
    private ParsedUsernamePresenceData? _usernamePresenceData;

    [ObservableProperty]
    private bool _isDismissing;

    public static event Action<string>? PivotSearchRequested;

    public bool IsEmailFinding => IsGitHubEmail || IsGravatarEmail || IsDisposableEmail || IsMxEmail;
    public bool IsIpFinding => IsIpGeo || IsIpReverseDns || IsIpShodan;
    public bool IsUsernameFinding => IsUsernameGithub || IsUsernameTelegram || IsUsernamePresence;

    public string Category
    {
        get
        {
            if (IsEmailFinding) return "Email";
            if (IsUsernameFinding) return "Username";
            if (IsIpFinding) return "IP";
            if (IsDns) return "DNS";
            if (IsWhois) return "Whois";
            if (IsSubdomain) return "Subdomain";
            if (IsHttpTech) return "Tech";
            return "Other";
        }
    }

    public string TypeBadgeText
    {
        get
        {
            if (IsGitHubEmail) return "GITHUB";
            if (IsGravatarEmail) return "GRAVATAR";
            if (IsDisposableEmail) return DisposableEmailData?.StatusBadgeText ?? "DISPOSABLE";
            if (IsMxEmail) return "MAIL SERVER";
            if (IsIpGeo) return "IP GEO";
            if (IsIpReverseDns) return "REVERSE DNS";
            if (IsIpShodan) return IpShodanData?.HasVulns == true ? "SHODAN (VULN)" : "SHODAN";
            if (IsUsernameGithub) return "GITHUB";
            if (IsUsernameTelegram) return "TELEGRAM";
            if (IsUsernamePresence) return !string.IsNullOrWhiteSpace(UsernamePresenceData?.Platform) ? UsernamePresenceData.Platform.ToUpperInvariant() : "PRESENCE";
            if (IsDns && !string.IsNullOrWhiteSpace(DnsData?.RecordType) &&
                !DnsData.RecordType.Equals("dns_record", StringComparison.OrdinalIgnoreCase))
            {
                return DnsData.RecordType;
            }
            if (IsWhois)
            {
                return "WHOIS";
            }
            if (IsSubdomain)
            {
                return "SUBDOMAIN";
            }
            if (IsHttpTech)
            {
                return HttpTechData != null && HttpTechData.StatusCode > 0
                    ? HttpTechData.StatusBadgeText
                    : "HTTP";
            }
            return Type;
        }
    }

    public string TypeBadgeColor
    {
        get
        {
            if (IsGitHubEmail) return "#8957E5";
            if (IsGravatarEmail) return "#00D2FF";
            if (IsDisposableEmail) return DisposableEmailData?.StatusBadgeColor ?? "#FF3366";
            if (IsMxEmail) return "#FF7A00";
            if (IsIpGeo) return "#00D2FF";
            if (IsIpReverseDns) return "#818CF8";
            if (IsIpShodan) return IpShodanData?.HasVulns == true ? "#F43F5E" : "#FFB800";
            if (IsUsernameGithub) return "#8957E5";
            if (IsUsernameTelegram) return "#2AABEE";
            if (IsUsernamePresence) return UsernamePresenceData?.PlatformBadgeForeground ?? "#00D2FF";
            if (IsDns && DnsData != null) return DnsData.RecordTypeColor;
            if (IsWhois) return "#C084FC";
            if (IsSubdomain) return "#00F0FF";
            if (IsHttpTech) return HttpTechData?.StatusColor ?? "#00E599";
            return "#00D2FF";
        }
    }

    public string TypeBadgeBackground
    {
        get
        {
            if (IsGitHubEmail) return "#258957E5";
            if (IsGravatarEmail) return "#2000D2FF";
            if (IsDisposableEmail) return DisposableEmailData?.StatusBadgeBackground ?? "#25FF3366";
            if (IsMxEmail) return "#25FF7A00";
            if (IsIpGeo) return "#2000D2FF";
            if (IsIpReverseDns) return "#20818CF8";
            if (IsIpShodan) return IpShodanData?.HasVulns == true ? "#25F43F5E" : "#25FFB800";
            if (IsUsernameGithub) return "#258957E5";
            if (IsUsernameTelegram) return "#252AABEE";
            if (IsUsernamePresence) return UsernamePresenceData?.PlatformBadgeBackground ?? "#2000D2FF";
            if (IsDns && DnsData != null) return DnsData.RecordTypeBackground;
            if (IsWhois) return "#25A855F7";
            if (IsSubdomain) return "#2000F0FF";
            if (IsHttpTech) return HttpTechData?.StatusBackground ?? "#2000E599";
            return "#2000D2FF";
        }
    }

    public FindingItemViewModel(FindingModel model)
    {
        Model = model;
        ParsePayload(model);
    }

    private void ParsePayload(FindingModel model)
    {
        FormattedJson = FormatJson(model.DataJson);

        var typeUpper = (model.Type ?? string.Empty).ToUpperInvariant();
        var providerUpper = (model.ProviderKey ?? string.Empty).ToUpperInvariant();

        bool isGitHub = typeUpper.Contains("GITHUB") || providerUpper.Contains("GITHUB");
        bool isGravatar = typeUpper.Contains("GRAVATAR") || providerUpper.Contains("GRAVATAR");
        bool isDisposable = typeUpper.Contains("DISPOSABLE") || providerUpper.Contains("DISPOSABLE") || typeUpper.Contains("BURNER");
        bool isMxEmail = typeUpper.Contains("MX-EMAIL") || typeUpper.Contains("MX_EMAIL") ||
                         (typeUpper.Contains("EMAIL") && typeUpper.Contains("MX")) ||
                         providerUpper.Contains("EMAIL-MX") || providerUpper.Contains("MX-EMAIL");

        bool isIpGeo = typeUpper.Contains("IP-GEO") || typeUpper.Contains("IP_GEO") || providerUpper.Contains("IP-GEO") ||
                       typeUpper.Equals("GEO_RECORD", StringComparison.OrdinalIgnoreCase);
        bool isIpRdns = typeUpper.Contains("IP-RDNS") || typeUpper.Contains("IP_RDNS") || typeUpper.Contains("RDNS_RECORD") ||
                        providerUpper.Contains("IP-RDNS") || providerUpper.Contains("RDNS");
        bool isIpShodan = typeUpper.Contains("IP-SHODAN") || typeUpper.Contains("IP_SHODAN") || typeUpper.Contains("SHODAN_RECORD") ||
                          providerUpper.Contains("IP-SHODAN") || providerUpper.Contains("SHODAN");

        bool isUsernameGithub = typeUpper.Contains("USERNAME-GITHUB") || typeUpper.Contains("USERNAME_GITHUB") ||
                                (typeUpper.Contains("GITHUB") && !typeUpper.Contains("EMAIL")) ||
                                (providerUpper.Contains("USERNAME-GITHUB") || (providerUpper.Contains("GITHUB") && !providerUpper.Contains("EMAIL")));
        bool isUsernameTelegram = typeUpper.Contains("USERNAME-TELEGRAM") || typeUpper.Contains("USERNAME_TELEGRAM") || typeUpper.Contains("TELEGRAM") ||
                                  providerUpper.Contains("USERNAME-TELEGRAM") || providerUpper.Contains("TELEGRAM");
        bool isUsernamePresence = typeUpper.Contains("USERNAME-PRESENCE") || typeUpper.Contains("USERNAME_PRESENCE") || typeUpper.Contains("PRESENCE") ||
                                  providerUpper.Contains("USERNAME-PRESENCE") || providerUpper.Contains("PRESENCE");

        bool isDnsType = typeUpper.Contains("DNS") ||
                         providerUpper.Contains("DNS") ||
                         typeUpper is "A" or "AAAA" or "MX" or "TXT" or "NS" or "CNAME" or "PTR" or "SOA";

        // 3. Recognize WHOIS findings
        bool isWhoisType = typeUpper.Contains("WHOIS") ||
                           providerUpper.Contains("WHOIS") ||
                           typeUpper.Contains("REGISTRAR");

        // 4. Recognize Subdomain findings
        bool isSubdomainType = typeUpper.Contains("SUBDOMAIN") ||
                               providerUpper.Contains("SUBDOMAIN") ||
                               providerUpper.Contains("CRT");

        // 5. Recognize HTTP Tech findings
        bool isHttpTechType = typeUpper.Contains("HTTP") ||
                              typeUpper.Contains("TECH") ||
                              providerUpper.Contains("HTTP") ||
                              providerUpper.Contains("TECH");

        if (isGitHub)
        {
            IsGitHubEmail = true;
            GitHubEmailData = ExtractGitHubEmailData(model);
        }
        else if (isGravatar)
        {
            IsGravatarEmail = true;
            GravatarEmailData = ExtractGravatarEmailData(model);
        }
        else if (isDisposable)
        {
            IsDisposableEmail = true;
            DisposableEmailData = ExtractDisposableEmailData(model);
        }
        else if (isMxEmail)
        {
            IsMxEmail = true;
            MxEmailData = ExtractMxEmailData(model);
        }
        else if (isIpGeo)
        {
            IsIpGeo = true;
            IpGeoData = ExtractIpGeoData(model);
        }
        else if (isIpRdns)
        {
            IsIpReverseDns = true;
            IpReverseDnsData = ExtractIpReverseDnsData(model);
        }
        else if (isIpShodan)
        {
            IsIpShodan = true;
            IpShodanData = ExtractIpShodanData(model);
        }
        else if (isUsernameGithub)
        {
            IsUsernameGithub = true;
            UsernameGithubData = ExtractUsernameGithubData(model);
        }
        else if (isUsernameTelegram)
        {
            IsUsernameTelegram = true;
            UsernameTelegramData = ExtractUsernameTelegramData(model);
        }
        else if (isUsernamePresence)
        {
            IsUsernamePresence = true;
            UsernamePresenceData = ExtractUsernamePresenceData(model);
        }
        else if (isDnsType)
        {
            IsDns = true;
            DnsData = ExtractDnsData(model);
        }
        else if (isWhoisType)
        {
            IsWhois = true;
            WhoisData = ExtractWhoisData(model);
        }
        else if (isSubdomainType)
        {
            IsSubdomain = true;
            SubdomainData = ExtractSubdomainData(model);
        }
        else if (isHttpTechType)
        {
            IsHttpTech = true;
            HttpTechData = ExtractHttpTechData(model);
        }
    }

    private ParsedGitHubEmailData ExtractGitHubEmailData(FindingModel model)
    {
        var data = new ParsedGitHubEmailData();
        if (string.IsNullOrWhiteSpace(model.DataJson)) return data;

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetLong(root, "Id", out var id) || TryGetLong(root, "id", out id))
                    data.Id = id;

                if (TryGetString(root, "Username", out var username) || TryGetString(root, "username", out username) || TryGetString(root, "login", out username))
                    data.Username = username;

                if (TryGetString(root, "ProfileId", out var profile) || TryGetString(root, "profileId", out profile) ||
                    TryGetString(root, "ProfileUrl", out profile) || TryGetString(root, "profileUrl", out profile) ||
                    TryGetString(root, "url", out profile) || TryGetString(root, "html_url", out profile))
                {
                    data.ProfileUrl = profile;
                }

                if (TryGetString(root, "AvatarUrl", out var avatar) || TryGetString(root, "avatarUrl", out avatar) || TryGetString(root, "avatar_url", out avatar))
                    data.AvatarUrl = avatar;

                if (TryGetString(root, "AccountType", out var accType) || TryGetString(root, "accountType", out accType) || TryGetString(root, "type", out accType))
                    data.AccountType = accType;
            }
        }
        catch { }

        if (string.IsNullOrWhiteSpace(data.ProfileUrl) && !string.IsNullOrWhiteSpace(data.Username))
        {
            data.ProfileUrl = $"https://github.com/{data.Username.TrimStart('@')}";
        }

        return data;
    }

    private ParsedGravatarEmailData ExtractGravatarEmailData(FindingModel model)
    {
        var data = new ParsedGravatarEmailData();
        if (string.IsNullOrWhiteSpace(model.DataJson)) return data;

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(root, "Username", out var u) || TryGetString(root, "username", out u))
                    data.Username = u;

                if (TryGetString(root, "DisplayName", out var d) || TryGetString(root, "displayName", out d) || TryGetString(root, "name", out d))
                    data.DisplayName = d;

                if (TryGetString(root, "AboutMe", out var a) || TryGetString(root, "aboutMe", out a) || TryGetString(root, "about", out a) || TryGetString(root, "bio", out a))
                    data.AboutMe = a;

                if (TryGetString(root, "Location", out var loc) || TryGetString(root, "location", out loc))
                    data.Location = loc;

                if (TryGetString(root, "ProfileUrl", out var p) || TryGetString(root, "profileUrl", out p) || TryGetString(root, "url", out p))
                    data.ProfileUrl = p;

                if (TryGetString(root, "AvatarUrl", out var av) || TryGetString(root, "avatarUrl", out av) || TryGetString(root, "avatar_url", out av))
                    data.AvatarUrl = av;
            }
        }
        catch { }

        if (string.IsNullOrWhiteSpace(data.ProfileUrl) && !string.IsNullOrWhiteSpace(data.Username))
        {
            data.ProfileUrl = $"https://gravatar.com/{data.Username}";
        }

        return data;
    }

    private ParsedDisposableEmailData ExtractDisposableEmailData(FindingModel model)
    {
        var data = new ParsedDisposableEmailData();
        if (string.IsNullOrWhiteSpace(model.DataJson)) return data;

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(root, "Email", out var email) || TryGetString(root, "email", out email))
                    data.Email = email;

                if (TryGetString(root, "Domain", out var domain) || TryGetString(root, "domain", out domain))
                    data.Domain = domain;

                if (TryGetBool(root, "IsDisposable", out var disp) || TryGetBool(root, "isDisposable", out disp) ||
                    TryGetBool(root, "disposable", out disp) || TryGetBool(root, "burner", out disp))
                {
                    data.IsDisposable = disp;
                }

                if (TryGetString(root, "Category", out var cat) || TryGetString(root, "category", out cat))
                    data.Category = cat;
            }
        }
        catch { }

        if (string.IsNullOrWhiteSpace(data.Domain) && !string.IsNullOrWhiteSpace(data.Email) && data.Email.Contains('@'))
        {
            data.Domain = data.Email.Split('@')[1];
        }

        return data;
    }

    private ParsedMxEmailData ExtractMxEmailData(FindingModel model)
    {
        var data = new ParsedMxEmailData();
        if (string.IsNullOrWhiteSpace(model.DataJson)) return data;

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(root, "EmailDomain", out var dom) || TryGetString(root, "emailDomain", out dom) || TryGetString(root, "domain", out dom))
                    data.EmailDomain = dom;

                if (TryGetString(root, "MailProviderName", out var prov) || TryGetString(root, "mailProviderName", out prov) ||
                    TryGetString(root, "providerName", out prov) || TryGetString(root, "provider", out prov))
                {
                    data.MailProviderName = prov;
                }

                if (TryGetBool(root, "IsEnterprise", out var ent) || TryGetBool(root, "isEnterprise", out ent) || TryGetBool(root, "enterprise", out ent))
                    data.IsEnterprise = ent;

                if (TryGetBool(root, "HasSpfProtection", out var spf) || TryGetBool(root, "hasSpfProtection", out spf) ||
                    TryGetBool(root, "spfProtection", out spf) || TryGetBool(root, "spf", out spf))
                {
                    data.HasSpfProtection = spf;
                }

                if (root.TryGetProperty("MxServers", out var mxProp) || root.TryGetProperty("mxServers", out mxProp) ||
                    root.TryGetProperty("mx", out mxProp) || root.TryGetProperty("servers", out mxProp))
                {
                    if (mxProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in mxProp.EnumerateArray())
                        {
                            var s = el.GetString();
                            if (!string.IsNullOrWhiteSpace(s)) data.MxServers.Add(s.Trim());
                        }
                    }
                    else if (mxProp.ValueKind == JsonValueKind.String)
                    {
                        var s = mxProp.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            data.MxServers.AddRange(s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        }
                    }
                }
            }
        }
        catch { }

        if (string.IsNullOrWhiteSpace(data.MailProviderName))
        {
            data.MailProviderName = "Почтовый сервер";
        }

        return data;
    }

    private ParsedDnsData ExtractDnsData(FindingModel model)
    {
        var data = new ParsedDnsData
        {
            RecordType = "DNS"
        };

        if (string.IsNullOrWhiteSpace(model.DataJson))
        {
            return data;
        }

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetInt(root, "type", out var typeInt))
                {
                    data.RecordType = GetDnsRecordTypeName(typeInt);
                }
                else if (TryGetString(root, "recordType", out var rt) || TryGetString(root, "type", out rt))
                {
                    data.RecordType = rt.ToUpperInvariant();
                }
                else if (!string.IsNullOrWhiteSpace(model.Type) && !model.Type.Equals("dns_record", StringComparison.OrdinalIgnoreCase))
                {
                    data.RecordType = model.Type.ToUpperInvariant();
                }

                if (TryGetString(root, "name", out var name) ||
                    TryGetString(root, "host", out name) ||
                    TryGetString(root, "domain", out name))
                {
                    data.Name = name.TrimEnd('.');
                }

                if (TryGetString(root, "data", out var val) ||
                    TryGetString(root, "value", out val) ||
                    TryGetString(root, "ip", out val) ||
                    TryGetString(root, "address", out val) ||
                    TryGetString(root, "target", out val) ||
                    TryGetString(root, "content", out val))
                {
                    data.Value = val.TrimEnd('.');
                }

                if (TryGetInt(root, "TTL", out var ttl) || TryGetInt(root, "ttl", out ttl))
                {
                    data.Ttl = ttl;
                }

                if (TryGetInt(root, "priority", out var priority) || TryGetInt(root, "preference", out priority))
                {
                    data.Priority = priority;
                }
            }
            else if (root.ValueKind == JsonValueKind.String)
            {
                data.Value = root.GetString() ?? string.Empty;
            }
        }
        catch
        {
            data.Value = model.DataJson;
        }

        if (string.IsNullOrWhiteSpace(data.Value))
        {
            data.Value = model.DataJson;
        }

        if (string.Equals(data.RecordType, "MX", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(data.Value))
        {
            var parts = data.Value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && int.TryParse(parts[0], out var prio))
            {
                data.Priority ??= prio;
                data.Value = parts[1].TrimEnd('.');
            }
        }

        return data;
    }

    private ParsedWhoisData ExtractWhoisData(FindingModel model)
    {
        var data = new ParsedWhoisData();

        if (string.IsNullOrWhiteSpace(model.DataJson))
        {
            return data;
        }

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(root, "ldhName", out var ldh) ||
                    TryGetString(root, "domainName", out ldh) ||
                    TryGetString(root, "domain", out ldh) ||
                    TryGetString(root, "handle", out ldh))
                {
                    data.DomainName = ldh;
                }

                if (root.TryGetProperty("status", out var statusProp))
                {
                    if (statusProp.ValueKind == JsonValueKind.Array)
                    {
                        var statusList = new List<string>();
                        foreach (var elem in statusProp.EnumerateArray())
                        {
                            var s = elem.GetString();
                            if (!string.IsNullOrWhiteSpace(s)) statusList.Add(s);
                        }
                        data.Status = string.Join(", ", statusList);
                    }
                    else if (statusProp.ValueKind == JsonValueKind.String)
                    {
                        data.Status = statusProp.GetString() ?? string.Empty;
                    }
                }

                if (root.TryGetProperty("events", out var eventsProp) && eventsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var evt in eventsProp.EnumerateArray())
                    {
                        if (evt.ValueKind != JsonValueKind.Object) continue;

                        TryGetString(evt, "eventAction", out var action);
                        TryGetString(evt, "eventDate", out var eventDate);

                        var formattedDate = FormatDateString(eventDate);

                        if (action.Contains("registration", StringComparison.OrdinalIgnoreCase) ||
                            action.Contains("created", StringComparison.OrdinalIgnoreCase))
                        {
                            data.CreatedDate = formattedDate;
                        }
                        else if (action.Contains("expiration", StringComparison.OrdinalIgnoreCase) ||
                                 action.Contains("expire", StringComparison.OrdinalIgnoreCase))
                        {
                            data.ExpiresDate = formattedDate;
                        }
                        else if (action.Contains("changed", StringComparison.OrdinalIgnoreCase) ||
                                 action.Contains("update", StringComparison.OrdinalIgnoreCase))
                        {
                            data.UpdatedDate = formattedDate;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(data.CreatedDate) &&
                    (TryGetString(root, "creationDate", out var cd) ||
                     TryGetString(root, "createdDate", out cd) ||
                     TryGetString(root, "registeredAt", out cd)))
                {
                    data.CreatedDate = FormatDateString(cd);
                }

                if (string.IsNullOrWhiteSpace(data.ExpiresDate) &&
                    (TryGetString(root, "expirationDate", out var ed) ||
                     TryGetString(root, "expiresDate", out ed) ||
                     TryGetString(root, "registryExpiryDate", out ed)))
                {
                    data.ExpiresDate = FormatDateString(ed);
                }

                if (root.TryGetProperty("entities", out var entitiesProp) && entitiesProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entity in entitiesProp.EnumerateArray())
                    {
                        if (entity.ValueKind != JsonValueKind.Object) continue;

                        var roles = new List<string>();
                        if (entity.TryGetProperty("roles", out var rolesProp) && rolesProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var r in rolesProp.EnumerateArray())
                            {
                                var rStr = r.GetString();
                                if (!string.IsNullOrWhiteSpace(rStr)) roles.Add(rStr.ToLowerInvariant());
                            }
                        }

                        if (roles.Contains("registrar"))
                        {
                            var regName = ExtractVcardFn(entity);
                            if (string.IsNullOrWhiteSpace(regName))
                            {
                                TryGetString(entity, "handle", out regName);
                            }
                            if (!string.IsNullOrWhiteSpace(regName))
                            {
                                data.Registrar = regName;
                            }
                        }

                        if (roles.Contains("registrant") || roles.Contains("administrative"))
                        {
                            var country = ExtractVcardCountry(entity);
                            if (!string.IsNullOrWhiteSpace(country))
                            {
                                data.RegistrantCountry = country;
                            }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(data.Registrar) &&
                    (TryGetString(root, "registrar", out var reg) ||
                     TryGetString(root, "registrarName", out reg) ||
                     TryGetString(root, "organization", out reg)))
                {
                    data.Registrar = reg;
                }

                if (string.IsNullOrWhiteSpace(data.RegistrantCountry) &&
                    (TryGetString(root, "country", out var countryVal) ||
                     TryGetString(root, "registrantCountry", out countryVal)))
                {
                    data.RegistrantCountry = countryVal;
                }

                if (root.TryGetProperty("nameservers", out var nsProp) ||
                    root.TryGetProperty("nameServers", out nsProp))
                {
                    if (nsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in nsProp.EnumerateArray())
                        {
                            if (elem.ValueKind == JsonValueKind.Object)
                            {
                                if (TryGetString(elem, "ldhName", out var nsName) ||
                                    TryGetString(elem, "name", out nsName))
                                {
                                    data.NameServers.Add(nsName);
                                }
                            }
                            else if (elem.ValueKind == JsonValueKind.String)
                            {
                                var nsStr = elem.GetString();
                                if (!string.IsNullOrWhiteSpace(nsStr)) data.NameServers.Add(nsStr);
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Raw JSON preserved
        }

        return data;
    }

    private static string ExtractVcardFn(JsonElement entity)
    {
        if (entity.TryGetProperty("vcardArray", out var vcardArray) && vcardArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in vcardArray.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Array) continue;

                foreach (var entry in item.EnumerateArray())
                {
                    if (entry.ValueKind == JsonValueKind.Array && entry.GetArrayLength() >= 4)
                    {
                        var tag = entry[0].ValueKind == JsonValueKind.String ? entry[0].GetString() : null;
                        if (string.Equals(tag, "fn", StringComparison.OrdinalIgnoreCase))
                        {
                            return entry[3].ValueKind == JsonValueKind.String
                                ? entry[3].GetString() ?? string.Empty
                                : entry[3].ToString();
                        }
                    }
                }
            }
        }
        return string.Empty;
    }

    private static string ExtractVcardCountry(JsonElement entity)
    {
        if (entity.TryGetProperty("vcardArray", out var vcardArray) && vcardArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in vcardArray.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Array) continue;

                foreach (var entry in item.EnumerateArray())
                {
                    if (entry.ValueKind == JsonValueKind.Array && entry.GetArrayLength() >= 4)
                    {
                        var tag = entry[0].ValueKind == JsonValueKind.String ? entry[0].GetString() : null;
                        if (string.Equals(tag, "adr", StringComparison.OrdinalIgnoreCase))
                        {
                            var adrVal = entry[3];
                            if (adrVal.ValueKind == JsonValueKind.Array)
                            {
                                var parts = adrVal.EnumerateArray().ToList();
                                if (parts.Count > 0)
                                {
                                    var last = parts[^1].GetString();
                                    if (!string.IsNullOrWhiteSpace(last)) return last;
                                }
                            }
                            else if (adrVal.ValueKind == JsonValueKind.String)
                            {
                                return adrVal.GetString() ?? string.Empty;
                            }
                        }
                    }
                }
            }
        }
        return string.Empty;
    }

    public static string GetDnsRecordTypeName(int type) => type switch
    {
        1 => "A",
        2 => "NS",
        5 => "CNAME",
        6 => "SOA",
        12 => "PTR",
        15 => "MX",
        16 => "TXT",
        28 => "AAAA",
        33 => "SRV",
        257 => "CAA",
        _ => $"TYPE{type}"
    };

    private static string FormatDateString(string? rawDate)
    {
        if (string.IsNullOrWhiteSpace(rawDate)) return string.Empty;
        if (DateTimeOffset.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
        {
            return dto.ToString("yyyy-MM-dd HH:mm UTC");
        }
        return rawDate;
    }

    private static bool TryGetString(JsonElement obj, string propertyName, out string result)
    {
        result = string.Empty;
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                result = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => prop.Value.ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => string.Empty
                };
                return !string.IsNullOrEmpty(result);
            }
        }
        return false;
    }

    private static bool TryGetInt(JsonElement obj, string propertyName, out int result)
    {
        result = 0;
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var val))
                {
                    result = val;
                    return true;
                }
                if (prop.Value.ValueKind == JsonValueKind.String && int.TryParse(prop.Value.GetString(), out var parsed))
                {
                    result = parsed;
                    return true;
                }
            }
        }
        return false;
    }

    private static bool TryGetLong(JsonElement obj, string propertyName, out long result)
    {
        result = 0;
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.TryGetInt64(out result)) return true;
                if (prop.Value.ValueKind == JsonValueKind.String && long.TryParse(prop.Value.GetString(), out result)) return true;
            }
        }
        return false;
    }

    private static bool TryGetBool(JsonElement obj, string propertyName, out bool result)
    {
        result = false;
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.ValueKind == JsonValueKind.True)
                {
                    result = true;
                    return true;
                }
                if (prop.Value.ValueKind == JsonValueKind.False)
                {
                    result = false;
                    return true;
                }
                if (prop.Value.ValueKind == JsonValueKind.String && bool.TryParse(prop.Value.GetString(), out var b))
                {
                    result = b;
                    return true;
                }
            }
        }
        return false;
    }

    private ParsedSubdomainData ExtractSubdomainData(FindingModel model)
    {
        var data = new ParsedSubdomainData();
        if (string.IsNullOrWhiteSpace(model.DataJson)) return data;

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(root, "subdomain", out var sub) ||
                    TryGetString(root, "domain", out sub) ||
                    TryGetString(root, "name", out sub) ||
                    TryGetString(root, "host", out sub))
                {
                    data.Subdomain = sub.TrimEnd('.');
                }

                if (TryGetString(root, "issuer", out var iss) ||
                    TryGetString(root, "issuerName", out iss) ||
                    TryGetString(root, "ca", out iss))
                {
                    data.Issuer = iss;
                }

                if (TryGetString(root, "validFrom", out var vf) ||
                    TryGetString(root, "notBefore", out vf) ||
                    TryGetString(root, "entryTimestamp", out vf))
                {
                    data.ValidFrom = vf;
                }

                if (TryGetString(root, "validTo", out var vt) ||
                    TryGetString(root, "notAfter", out vt))
                {
                    data.ValidTo = vt;
                }
            }
            else if (root.ValueKind == JsonValueKind.String)
            {
                data.Subdomain = root.GetString() ?? string.Empty;
            }
        }
        catch
        {
            data.Subdomain = model.DataJson;
        }

        if (string.IsNullOrWhiteSpace(data.Subdomain))
        {
            data.Subdomain = model.DataJson;
        }

        return data;
    }

    private ParsedHttpTechData ExtractHttpTechData(FindingModel model)
    {
        var data = new ParsedHttpTechData();
        if (string.IsNullOrWhiteSpace(model.DataJson)) return data;

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(root, "url", out var u) || TryGetString(root, "target", out u))
                {
                    data.Url = u;
                }

                if (TryGetInt(root, "statusCode", out var sc) || TryGetInt(root, "status", out sc))
                {
                    data.StatusCode = sc;
                }

                if (TryGetString(root, "server", out var s))
                {
                    data.Server = s;
                }

                if (TryGetString(root, "poweredBy", out var pb) || TryGetString(root, "xPoweredBy", out pb))
                {
                    data.PoweredBy = pb;
                }

                if (TryGetString(root, "title", out var title) || TryGetString(root, "pageTitle", out title))
                {
                    data.Title = title;
                }

                if (TryGetBool(root, "hasHsts", out var hsts) || TryGetBool(root, "hsts", out hsts))
                {
                    data.HasHsts = hsts;
                }

                if (TryGetBool(root, "hasCsp", out var csp) || TryGetBool(root, "csp", out csp))
                {
                    data.HasCsp = csp;
                }

                if (TryGetBool(root, "hasXFrameOptions", out var xf) || TryGetBool(root, "xFrameOptions", out xf))
                {
                    data.HasXFrameOptions = xf;
                }

                if (TryGetString(root, "contentType", out var ct))
                {
                    data.ContentType = ct;
                }
            }
        }
        catch
        {
            // Raw JSON preserved
        }

        return data;
    }

    private ParsedIpGeoData ExtractIpGeoData(FindingModel model)
    {
        var data = new ParsedIpGeoData();
        if (string.IsNullOrWhiteSpace(model.DataJson)) return data;

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(root, "Ip", out var ip) || TryGetString(root, "query", out ip)) data.Ip = ip;
                if (TryGetString(root, "Country", out var country)) data.Country = country;
                if (TryGetString(root, "CountryCode", out var cc)) data.CountryCode = cc;
                if (TryGetString(root, "Region", out var region) || TryGetString(root, "regionName", out region)) data.Region = region;
                if (TryGetString(root, "City", out var city)) data.City = city;
                if (TryGetDouble(root, "Latitude", out var lat) || TryGetDouble(root, "lat", out lat)) data.Latitude = lat;
                if (TryGetDouble(root, "Longitude", out var lon) || TryGetDouble(root, "lon", out lon)) data.Longitude = lon;
                if (TryGetString(root, "Timezone", out var tz)) data.Timezone = tz;
                if (TryGetString(root, "Isp", out var isp)) data.Isp = isp;
                if (TryGetString(root, "Org", out var org)) data.Org = org;
                if (TryGetString(root, "Asn", out var asn) || TryGetString(root, "as", out asn)) data.Asn = asn;
                if (TryGetBool(root, "IsMobile", out var mobile) || TryGetBool(root, "mobile", out mobile)) data.IsMobile = mobile;
                if (TryGetBool(root, "IsProxy", out var proxy) || TryGetBool(root, "proxy", out proxy)) data.IsProxy = proxy;
                if (TryGetBool(root, "IsHosting", out var hosting) || TryGetBool(root, "hosting", out hosting)) data.IsHosting = hosting;
            }
        }
        catch { }

        return data;
    }

    private ParsedIpReverseDnsData ExtractIpReverseDnsData(FindingModel model)
    {
        var data = new ParsedIpReverseDnsData();
        if (string.IsNullOrWhiteSpace(model.DataJson)) return data;

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(root, "Ip", out var ip)) data.Ip = ip;
                if (TryGetString(root, "ArpaQuery", out var arpa) || TryGetString(root, "arpa", out arpa)) data.ArpaQuery = arpa;
                if (TryGetStringList(root, "Hostnames", out var hosts) || TryGetStringList(root, "names", out hosts) || TryGetStringList(root, "domains", out hosts))
                {
                    data.Hostnames = hosts;
                }
            }
        }
        catch { }

        return data;
    }

    private ParsedUsernameGithubData ExtractUsernameGithubData(FindingModel model)
    {
        var data = new ParsedUsernameGithubData();
        if (string.IsNullOrWhiteSpace(model.DataJson)) return data;

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetLong(root, "Id", out var id) || TryGetLong(root, "id", out id)) data.Id = id;
                if (TryGetString(root, "Username", out var u) || TryGetString(root, "username", out u) || TryGetString(root, "login", out u)) data.Username = u;
                if (TryGetString(root, "Name", out var n) || TryGetString(root, "name", out n)) data.Name = n;
                if (TryGetString(root, "ProfileUrl", out var p) || TryGetString(root, "profileUrl", out p) || TryGetString(root, "html_url", out p)) data.ProfileUrl = p;
                if (TryGetString(root, "AvatarUrl", out var a) || TryGetString(root, "avatarUrl", out a) || TryGetString(root, "avatar_url", out a)) data.AvatarUrl = a;
                if (TryGetString(root, "Bio", out var b) || TryGetString(root, "bio", out b)) data.Bio = b;
                if (TryGetString(root, "Company", out var c) || TryGetString(root, "company", out c)) data.Company = c;
                if (TryGetString(root, "Location", out var l) || TryGetString(root, "location", out l)) data.Location = l;
                if (TryGetString(root, "Email", out var e) || TryGetString(root, "email", out e)) data.Email = e;
                if (TryGetString(root, "Blog", out var bl) || TryGetString(root, "blog", out bl)) data.Blog = bl;
                if (TryGetString(root, "Twitter", out var tw) || TryGetString(root, "twitter", out tw) || TryGetString(root, "twitter_username", out tw)) data.Twitter = tw;
                if (TryGetInt(root, "PublicRepos", out var pr) || TryGetInt(root, "publicRepos", out pr) || TryGetInt(root, "public_repos", out pr)) data.PublicRepos = pr;
                if (TryGetInt(root, "Followers", out var fl) || TryGetInt(root, "followers", out fl)) data.Followers = fl;
            }
        }
        catch { }

        if (string.IsNullOrWhiteSpace(data.ProfileUrl) && !string.IsNullOrWhiteSpace(data.Username))
        {
            data.ProfileUrl = $"https://github.com/{data.Username.TrimStart('@')}";
        }

        return data;
    }

    private ParsedUsernameTelegramData ExtractUsernameTelegramData(FindingModel model)
    {
        var data = new ParsedUsernameTelegramData();
        if (string.IsNullOrWhiteSpace(model.DataJson)) return data;

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(root, "Username", out var u) || TryGetString(root, "username", out u)) data.Username = u;
                if (TryGetString(root, "Title", out var t) || TryGetString(root, "title", out t) || TryGetString(root, "name", out t)) data.Title = t;
                if (TryGetString(root, "Description", out var d) || TryGetString(root, "description", out d) || TryGetString(root, "bio", out d)) data.Description = d;
                if (TryGetString(root, "AvatarUrl", out var a) || TryGetString(root, "avatarUrl", out a) || TryGetString(root, "avatar_url", out a) || TryGetString(root, "photo", out a)) data.AvatarUrl = a;
                if (TryGetString(root, "Url", out var url) || TryGetString(root, "url", out url) || TryGetString(root, "link", out url)) data.Url = url;
            }
        }
        catch { }

        if (string.IsNullOrWhiteSpace(data.Url) && !string.IsNullOrWhiteSpace(data.Username))
        {
            data.Url = $"https://t.me/{data.Username.TrimStart('@')}";
        }

        return data;
    }

    private ParsedUsernamePresenceData ExtractUsernamePresenceData(FindingModel model)
    {
        var data = new ParsedUsernamePresenceData();
        if (string.IsNullOrWhiteSpace(model.DataJson)) return data;

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(root, "Username", out var u) || TryGetString(root, "username", out u)) data.Username = u;
                if (TryGetString(root, "Platform", out var p) || TryGetString(root, "platform", out p)) data.Platform = p;
                if (TryGetString(root, "Url", out var url) || TryGetString(root, "url", out url) || TryGetString(root, "link", out url)) data.Url = url;
                if (TryGetBool(root, "Exists", out var ex) || TryGetBool(root, "exists", out ex)) data.Exists = ex;
                if (TryGetString(root, "Details", out var dt) || TryGetString(root, "details", out dt)) data.Details = dt;
            }
        }
        catch { }

        return data;
    }

    private ParsedIpShodanData ExtractIpShodanData(FindingModel model)
    {
        var data = new ParsedIpShodanData();
        if (string.IsNullOrWhiteSpace(model.DataJson)) return data;

        try
        {
            using var doc = JsonDocument.Parse(model.DataJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(root, "Ip", out var ip)) data.Ip = ip;
                if (TryGetIntList(root, "Ports", out var ports)) data.Ports = ports;
                if (TryGetStringList(root, "Hostnames", out var hostnames)) data.Hostnames = hostnames;
                if (TryGetStringList(root, "Tags", out var tags)) data.Tags = tags;
                if (TryGetStringList(root, "Cpes", out var cpes)) data.Cpes = cpes;
                if (TryGetStringList(root, "Vulnerabilities", out var vulns) || TryGetStringList(root, "vulns", out vulns))
                {
                    data.Vulnerabilities = vulns;
                }
            }
        }
        catch { }

        return data;
    }

    private static bool TryGetDouble(JsonElement obj, string propertyName, out double result)
    {
        result = 0;
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.TryGetDouble(out result)) return true;
                if (prop.Value.ValueKind == JsonValueKind.String && double.TryParse(prop.Value.GetString(), CultureInfo.InvariantCulture, out result)) return true;
            }
        }
        return false;
    }

    private static bool TryGetStringList(JsonElement obj, string propertyName, out List<string> list)
    {
        list = [];
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.Value.EnumerateArray())
                {
                    var str = item.GetString();
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        list.Add(str.Trim());
                    }
                }
                return true;
            }
        }
        return false;
    }

    private static bool TryGetIntList(JsonElement obj, string propertyName, out List<int> list)
    {
        list = [];
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.Value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var num))
                    {
                        list.Add(num);
                    }
                    else if (item.ValueKind == JsonValueKind.String && int.TryParse(item.GetString(), out var parsed))
                    {
                        list.Add(parsed);
                    }
                }
                return true;
            }
        }
        return false;
    }

    [RelayCommand]
    private void PivotSearch(string? target)
    {
        if (string.IsNullOrWhiteSpace(target)) return;
        PivotSearchRequested?.Invoke(target.Trim());
    }

    private static string FormatJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return raw;
        }
    }

    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    [RelayCommand]
    private void CopyJson()
    {
        if (!string.IsNullOrEmpty(FormattedJson))
        {
            Clipboard.SetText(FormattedJson);
        }
    }

    [RelayCommand]
    private void CopyValue(string? value)
    {
        var toCopy = !string.IsNullOrWhiteSpace(value) ? value : DnsData?.Value;
        if (!string.IsNullOrWhiteSpace(toCopy))
        {
            Clipboard.SetText(toCopy);
        }
    }

    [RelayCommand]
    private void OpenUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            var psi = new ProcessStartInfo(url)
            {
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch { }
    }

    [RelayCommand]
    private void PreviewAvatar()
    {
        var loc = LocalizationService.Instance;
        string title;
        string? avatarUrl;
        string displayName;
        string? subtitle;
        string? profileUrl;
        Color accentColor;

        if (IsGitHubEmail && GitHubEmailData != null)
        {
            title = loc.GetString("Avatar_Dialog_Title_GitHub");
            avatarUrl = GitHubEmailData.AvatarUrl;
            displayName = !string.IsNullOrWhiteSpace(GitHubEmailData.FormattedUsername) ? GitHubEmailData.FormattedUsername : (GitHubEmailData.Username ?? string.Empty);
            subtitle = !string.IsNullOrWhiteSpace(GitHubEmailData.AccountType)
                ? $"GitHub {GitHubEmailData.AccountType} • ID: {GitHubEmailData.Id}"
                : $"GitHub User • ID: {GitHubEmailData.Id}";
            profileUrl = GitHubEmailData.ProfileUrl;
            accentColor = Color.FromRgb(137, 87, 229);
        }
        else if (IsGravatarEmail && GravatarEmailData != null)
        {
            title = loc.GetString("Avatar_Dialog_Title_Gravatar");
            avatarUrl = GravatarEmailData.AvatarUrl;
            displayName = !string.IsNullOrWhiteSpace(GravatarEmailData.EffectiveName)
                ? GravatarEmailData.EffectiveName
                : (!string.IsNullOrWhiteSpace(GravatarEmailData.FormattedUsername) ? GravatarEmailData.FormattedUsername : (GravatarEmailData.Username ?? string.Empty));
            subtitle = !string.IsNullOrWhiteSpace(GravatarEmailData.Location)
                ? GravatarEmailData.Location
                : GravatarEmailData.FormattedUsername;
            profileUrl = GravatarEmailData.ProfileUrl;
            accentColor = Color.FromRgb(0, 210, 255);
        }
        else if (IsUsernameGithub && UsernameGithubData != null)
        {
            title = loc.GetString("Avatar_Dialog_Title_GitHub");
            avatarUrl = UsernameGithubData.AvatarUrl;
            displayName = UsernameGithubData.DisplayTitle;
            subtitle = UsernameGithubData.FormattedUsername;
            profileUrl = UsernameGithubData.ProfileUrl;
            accentColor = Color.FromRgb(137, 87, 229);
        }
        else if (IsUsernameTelegram && UsernameTelegramData != null)
        {
            title = loc.GetString("Avatar_Dialog_Title_Telegram");
            avatarUrl = UsernameTelegramData.AvatarUrl;
            displayName = UsernameTelegramData.DisplayTitle;
            subtitle = UsernameTelegramData.FormattedUsername;
            profileUrl = UsernameTelegramData.Url;
            accentColor = Color.FromRgb(42, 171, 238);
        }
        else
        {
            return;
        }

        AvatarPreviewWindow.Show(
            Application.Current?.MainWindow,
            title,
            avatarUrl,
            displayName,
            subtitle,
            profileUrl,
            accentColor);
    }
}
