using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeridianClient.Models;

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
    private ParsedDnsData? _dnsData;

    [ObservableProperty]
    private ParsedWhoisData? _whoisData;

    [ObservableProperty]
    private ParsedSubdomainData? _subdomainData;

    [ObservableProperty]
    private ParsedHttpTechData? _httpTechData;

    [ObservableProperty]
    private bool _isDismissing;

    public string Category
    {
        get
        {
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
            if (IsDns && DnsData != null) return DnsData.RecordTypeColor;
            if (IsWhois) return "#C084FC"; // Neon Purple
            if (IsSubdomain) return "#00F0FF"; // Electric Cyan / Aqua
            if (IsHttpTech) return HttpTechData?.StatusColor ?? "#00E599";
            return "#00D2FF";
        }
    }

    public string TypeBadgeBackground
    {
        get
        {
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

        // Recognize DNS findings (type "dns_record", "DNS", "A", "NS", etc. or provider "domain-dns")
        bool isDnsType = typeUpper.Contains("DNS") ||
                         providerUpper.Contains("DNS") ||
                         typeUpper is "A" or "AAAA" or "MX" or "TXT" or "NS" or "CNAME" or "PTR" or "SOA";

        // Recognize WHOIS findings (type "whois_record", "WHOIS" or provider "domain-whois")
        bool isWhoisType = typeUpper.Contains("WHOIS") ||
                           providerUpper.Contains("WHOIS") ||
                           typeUpper.Contains("REGISTRAR");

        // Recognize Subdomain findings (type "subdomain_record", provider "domain-subdomains" or crt.sh)
        bool isSubdomainType = typeUpper.Contains("SUBDOMAIN") ||
                               providerUpper.Contains("SUBDOMAIN") ||
                               providerUpper.Contains("CRT");

        // Recognize HTTP Tech findings (type "http_tech_record", provider "domain-http-tech")
        bool isHttpTechType = typeUpper.Contains("HTTP") ||
                              typeUpper.Contains("TECH") ||
                              providerUpper.Contains("HTTP") ||
                              providerUpper.Contains("TECH");

        if (isDnsType)
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
                // DNS Record Type (can be int like Cloudflare DNS, or string like "A", "NS")
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

                // Name / Host
                if (TryGetString(root, "name", out var name) ||
                    TryGetString(root, "host", out name) ||
                    TryGetString(root, "domain", out name))
                {
                    data.Name = name.TrimEnd('.');
                }

                // Value / Data (e.g. IP address or NS hostname)
                if (TryGetString(root, "data", out var val) ||
                    TryGetString(root, "value", out val) ||
                    TryGetString(root, "ip", out val) ||
                    TryGetString(root, "address", out val) ||
                    TryGetString(root, "target", out val) ||
                    TryGetString(root, "content", out val))
                {
                    data.Value = val.TrimEnd('.');
                }

                // TTL (case-insensitive, Cloudflare uses "TTL")
                if (TryGetInt(root, "TTL", out var ttl) || TryGetInt(root, "ttl", out ttl))
                {
                    data.Ttl = ttl;
                }

                // Priority (for MX / SRV records)
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
                // 1. Domain Name (RDAP "ldhName" or standard "domainName" / "domain")
                if (TryGetString(root, "ldhName", out var ldh) ||
                    TryGetString(root, "domainName", out ldh) ||
                    TryGetString(root, "domain", out ldh) ||
                    TryGetString(root, "handle", out ldh))
                {
                    data.DomainName = ldh;
                }

                // 2. Status (can be string or array in RDAP)
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

                // 3. RDAP Events (Registration, Expiration, Last Changed)
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

                // Traditional Whois Dates (fallback if events was not present)
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

                // 4. RDAP Entities (Registrar, Registrant Org/Country)
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

                        // Extract registrar name
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

                        // Extract registrant country / org
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

                // Fallback traditional Registrar property
                if (string.IsNullOrWhiteSpace(data.Registrar) &&
                    (TryGetString(root, "registrar", out var reg) ||
                     TryGetString(root, "registrarName", out reg) ||
                     TryGetString(root, "organization", out reg)))
                {
                    data.Registrar = reg;
                }

                // Fallback traditional Country
                if (string.IsNullOrWhiteSpace(data.RegistrantCountry) &&
                    (TryGetString(root, "country", out var countryVal) ||
                     TryGetString(root, "registrantCountry", out countryVal)))
                {
                    data.RegistrantCountry = countryVal;
                }

                // 5. Nameservers (RDAP "nameservers" array with "ldhName")
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
}
