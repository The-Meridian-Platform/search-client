using MeridianClient.Models;
using MeridianClient.ViewModels;

namespace MeridianClient.Tests;

public class FindingItemViewModelTests
{
    [Fact]
    public void ParsePayload_DnsRecord_ExtractsDnsDataProperly()
    {
        // Arrange
        var model = new FindingModel
        {
            FindingId = "finding-1",
            ProviderKey = "DnsProvider",
            Type = "A",
            DataJson = """{"name":"ayanami-connect.online","value":"185.199.108.153","ttl":300}""",
            CreatedAt = DateTime.UtcNow.ToString("O")
        };

        // Act
        var vm = new FindingItemViewModel(model);

        // Assert
        Assert.True(vm.IsDns);
        Assert.False(vm.IsWhois);
        Assert.Equal("DNS", vm.Category);
        Assert.NotNull(vm.DnsData);
        Assert.Equal("A", vm.DnsData.RecordType);
        Assert.Equal("ayanami-connect.online", vm.DnsData.Name);
        Assert.Equal("185.199.108.153", vm.DnsData.Value);
        Assert.Equal(300, vm.DnsData.Ttl);
    }

    [Fact]
    public void ParsePayload_DnsMxRecord_ExtractsPriority()
    {
        // Arrange
        var model = new FindingModel
        {
            FindingId = "finding-2",
            ProviderKey = "DNS",
            Type = "MX",
            DataJson = """{"name":"ayanami-connect.online","value":"mail.ayanami-connect.online","priority":10,"ttl":3600}""",
            CreatedAt = DateTime.UtcNow.ToString("O")
        };

        // Act
        var vm = new FindingItemViewModel(model);

        // Assert
        Assert.True(vm.IsDns);
        Assert.Equal("MX", vm.DnsData?.RecordType);
        Assert.Equal("mail.ayanami-connect.online", vm.DnsData?.Value);
        Assert.Equal(10, vm.DnsData?.Priority);
        Assert.Equal(3600, vm.DnsData?.Ttl);
    }

    [Fact]
    public void ParsePayload_WhoisRecord_ExtractsRegistrarAndDates()
    {
        // Arrange
        var model = new FindingModel
        {
            FindingId = "finding-3",
            ProviderKey = "WhoisService",
            Type = "WHOIS",
            DataJson = """
            {
                "domainName": "ayanami-connect.online",
                "registrar": "NameCheap, Inc.",
                "createdDate": "2024-01-15T00:00:00Z",
                "expiresDate": "2025-01-15T00:00:00Z",
                "status": "clientTransferProhibited",
                "registrantCountry": "US"
            }
            """,
            CreatedAt = DateTime.UtcNow.ToString("O")
        };

        // Act
        var vm = new FindingItemViewModel(model);

        // Assert
        Assert.True(vm.IsWhois);
        Assert.False(vm.IsDns);
        Assert.Equal("Whois", vm.Category);
        Assert.NotNull(vm.WhoisData);
        Assert.Equal("ayanami-connect.online", vm.WhoisData.DomainName);
        Assert.Equal("NameCheap, Inc.", vm.WhoisData.Registrar);
        Assert.Contains("2024-01-15", vm.WhoisData.CreatedDate);
        Assert.Contains("2025-01-15", vm.WhoisData.ExpiresDate);
        Assert.Equal("clientTransferProhibited", vm.WhoisData.Status);
        Assert.Equal("US", vm.WhoisData.RegistrantCountry);
    }

    [Fact]
    public void ParsePayload_CloudflareDnsIntegerType_ExtractsCorrectly()
    {
        // Arrange (exact payload from user's screenshot)
        var model = new FindingModel
        {
            FindingId = "finding-cf-1",
            ProviderKey = "domain-dns",
            Type = "dns_record",
            DataJson = """{"TTL": 600, "data": "ns2.timeweb.ru.", "name": "ayanami-connect.online", "type": 2}""",
            CreatedAt = "2026-10-31T21:04:41.981952+00:00"
        };

        // Act
        var vm = new FindingItemViewModel(model);

        // Assert
        Assert.True(vm.IsDns);
        Assert.Equal("NS", vm.TypeBadgeText);
        Assert.NotNull(vm.DnsData);
        Assert.Equal("NS", vm.DnsData.RecordType);
        Assert.Equal("ns2.timeweb.ru", vm.DnsData.Value);
        Assert.Equal("ayanami-connect.online", vm.DnsData.Name);
        Assert.Equal(600, vm.DnsData.Ttl);
    }

    [Fact]
    public void ParsePayload_RdapWhois_ExtractsEntitiesAndEvents()
    {
        // Arrange (RDAP Whois format from backend)
        var model = new FindingModel
        {
            FindingId = "finding-rdap-1",
            ProviderKey = "domain-whois",
            Type = "whois_record",
            DataJson = """
            {
                "ldhName": "ayanami-connect.online",
                "status": ["clientTransferProhibited", "active"],
                "events": [
                    {"eventAction": "registration", "eventDate": "2024-05-12T10:00:00Z"},
                    {"eventAction": "expiration", "eventDate": "2027-05-12T10:00:00Z"}
                ],
                "nameservers": [
                    {"ldhName": "ns1.timeweb.ru"},
                    {"ldhName": "ns2.timeweb.ru"}
                ],
                "entities": [
                    {
                        "roles": ["registrar"],
                        "vcardArray": ["vcard", [["version", {}, "text", "4.0"], ["fn", {}, "text", "Timeweb Cloud LLC"]]]
                    }
                ]
            }
            """,
            CreatedAt = "2026-10-31T21:04:42.440578+00:00"
        };

        // Act
        var vm = new FindingItemViewModel(model);

        // Assert
        Assert.True(vm.IsWhois);
        Assert.Equal("WHOIS", vm.TypeBadgeText);
        Assert.NotNull(vm.WhoisData);
        Assert.Equal("ayanami-connect.online", vm.WhoisData.DomainName);
        Assert.Equal("Timeweb Cloud LLC", vm.WhoisData.Registrar);
        Assert.Contains("2024-05-12", vm.WhoisData.CreatedDate);
        Assert.Contains("2027-05-12", vm.WhoisData.ExpiresDate);
        Assert.Contains("clientTransferProhibited", vm.WhoisData.Status);
        Assert.Equal(2, vm.WhoisData.NameServers.Count);
        Assert.Contains("ns1.timeweb.ru", vm.WhoisData.NameServers);
    }

    [Fact]
    public void FormatJson_ProducesIndentedJson()
    {
        // Arrange
        var model = new FindingModel
        {
            FindingId = "finding-4",
            ProviderKey = "Custom",
            Type = "CustomInfo",
            DataJson = """{"foo":"bar","num":123}"""
        };

        // Act
        var vm = new FindingItemViewModel(model);

        // Assert
        Assert.Contains("\n", vm.FormattedJson);
        Assert.Contains("\"foo\": \"bar\"", vm.FormattedJson);
    }

    [Fact]
    public void ParsePayload_SubdomainRecord_ExtractsSubdomainAndIssuer()
    {
        // Arrange
        var model = new FindingModel
        {
            FindingId = "finding-sub-1",
            ProviderKey = "domain-subdomains",
            Type = "subdomain_record",
            DataJson = """
            {
                "subdomain": "api.example.com",
                "issuer": "Let's Encrypt Authority X3",
                "validFrom": "2026-01-01",
                "validTo": "2026-04-01"
            }
            """
        };

        // Act
        var vm = new FindingItemViewModel(model);

        // Assert
        Assert.True(vm.IsSubdomain);
        Assert.False(vm.IsDns);
        Assert.False(vm.IsWhois);
        Assert.Equal("Subdomain", vm.Category);
        Assert.Equal("SUBDOMAIN", vm.TypeBadgeText);
        Assert.NotNull(vm.SubdomainData);
        Assert.Equal("api.example.com", vm.SubdomainData.Subdomain);
        Assert.Equal("Let's Encrypt Authority X3", vm.SubdomainData.Issuer);
        Assert.Equal("2026-01-01", vm.SubdomainData.ValidFrom);
        Assert.Equal("2026-04-01", vm.SubdomainData.ValidTo);
    }

    [Fact]
    public void ParsePayload_HttpTechRecord_ExtractsSecurityHeadersAndStatus()
    {
        // Arrange
        var model = new FindingModel
        {
            FindingId = "finding-http-1",
            ProviderKey = "domain-http-tech",
            Type = "http_tech_record",
            DataJson = """
            {
                "url": "https://example.com",
                "statusCode": 200,
                "server": "nginx/1.24.0",
                "poweredBy": "ASP.NET Core",
                "title": "Example Domain",
                "hasHsts": true,
                "hasCsp": false,
                "hasXFrameOptions": true,
                "contentType": "text/html"
            }
            """
        };

        // Act
        var vm = new FindingItemViewModel(model);

        // Assert
        Assert.True(vm.IsHttpTech);
        Assert.False(vm.IsDns);
        Assert.False(vm.IsSubdomain);
        Assert.Equal("Tech", vm.Category);
        Assert.Equal("200 OK", vm.TypeBadgeText);
        Assert.NotNull(vm.HttpTechData);
        Assert.Equal("https://example.com", vm.HttpTechData.Url);
        Assert.Equal(200, vm.HttpTechData.StatusCode);
        Assert.Equal("nginx/1.24.0", vm.HttpTechData.Server);
        Assert.Equal("ASP.NET Core", vm.HttpTechData.PoweredBy);
        Assert.Equal("Example Domain", vm.HttpTechData.Title);
        Assert.True(vm.HttpTechData.HasHsts);
        Assert.False(vm.HttpTechData.HasCsp);
        Assert.True(vm.HttpTechData.HasXFrameOptions);
        Assert.Equal("text/html", vm.HttpTechData.ContentType);
    }
}
