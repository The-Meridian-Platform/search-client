using MeridianClient.Models;
using MeridianClient.Services;
using MeridianClient.ViewModels;

namespace MeridianClient.Tests;

public class SearchViewModelTests
{
    public class DummyApiClient : IMeridianApiClient
    {
        public Task<CreateSearchSessionResponse> CreateSessionAsync(string targetValue, string targetType = "Domain", CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CreateSearchSessionResponse { SearchSessionId = Guid.NewGuid().ToString() });
        }

        public Task<List<SearchSessionModel>> GetSessionsHistoryAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<SearchSessionModel>());
        }

        public Task<SearchSessionModel?> GetSessionDetailsAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<SearchSessionModel?>(null);
        }

        public Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<(bool Success, string Message)> TestConnectionAsync(string? customUrl = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((true, "OK"));
        }
    }

    public class DummyStreamClient : IMeridianStreamClient
    {
        public async IAsyncEnumerable<Meridian.Client.Grpc.SearchStreamResponse> SubscribeToSessionAsync(string sessionId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield break;
        }
    }

    [Fact]
    public void LoadExistingSession_PopulatesFindingsAndMetrics()
    {
        // Arrange
        var logService = new Services.LogService();
        var vm = new SearchViewModel(new DummyApiClient(), new DummyStreamClient(), logService);
        var session = new SearchSessionModel
        {
            Id = Guid.NewGuid().ToString(),
            Target = new SearchTarget { Value = "ayanami-connect.online", TargetType = "Domain" },
            StatusRaw = "Completed",
            Finding = new List<FindingModel>
            {
                new()
                {
                    Type = "A",
                    ProviderKey = "Dns",
                    DataJson = """{"value":"1.2.3.4"}"""
                },
                new()
                {
                    Type = "WHOIS",
                    ProviderKey = "WhoisService",
                    DataJson = """{"domainName":"ayanami-connect.online","registrar":"Example"}"""
                }
            }
        };

        // Act
        vm.LoadExistingSession(session);

        // Assert
        Assert.Equal("ayanami-connect.online", vm.TargetInput);
        Assert.Equal("Completed", vm.SessionStatus);
        Assert.Equal(2, vm.TotalFindingsCount);
        Assert.Equal(1, vm.DnsFindingsCount);
        Assert.Equal(1, vm.WhoisFindingsCount);
        Assert.Equal(0, vm.SubdomainFindingsCount);
        Assert.Equal(0, vm.HttpTechFindingsCount);
        Assert.Equal(2, vm.Findings.Count);
    }

    [Fact]
    public void LoadExistingSession_PopulatesSubdomainAndHttpTechCounts()
    {
        // Arrange
        var logService = new Services.LogService();
        var vm = new SearchViewModel(new DummyApiClient(), new DummyStreamClient(), logService);
        var session = new SearchSessionModel
        {
            Id = Guid.NewGuid().ToString(),
            Target = new SearchTarget { Value = "example.com", TargetType = "Domain" },
            StatusRaw = "Completed",
            FindingsList = new List<FindingModel>
            {
                new() { Type = "subdomain_record", ProviderKey = "domain-subdomains", DataJson = """{"subdomain":"api.example.com"}""" },
                new() { Type = "http_tech_record", ProviderKey = "domain-http-tech", DataJson = """{"url":"https://example.com","statusCode":200}""" }
            }
        };

        // Act
        vm.LoadExistingSession(session);

        // Assert
        Assert.Equal(2, vm.TotalFindingsCount);
        Assert.Equal(1, vm.SubdomainFindingsCount);
        Assert.Equal(1, vm.HttpTechFindingsCount);

        // Test Filtering by tab
        vm.SetFilter("Subdomains");
        var filteredList = vm.FindingsView.Cast<FindingItemViewModel>().ToList();
        Assert.Single(filteredList);
        Assert.True(filteredList[0].IsSubdomain);

        vm.SetFilter("Tech");
        filteredList = vm.FindingsView.Cast<FindingItemViewModel>().ToList();
        Assert.Single(filteredList);
        Assert.True(filteredList[0].IsHttpTech);
    }

    [Fact]
    public async Task ClearResultsAsync_ResetsAllFieldsAndAnimates()
    {
        // Arrange
        var logService = new Services.LogService();
        var vm = new SearchViewModel(new DummyApiClient(), new DummyStreamClient(), logService);
        vm.TargetInput = "example.com";
        vm.SearchText = "query";
        vm.Findings.Add(new FindingItemViewModel(new FindingModel { Type = "A" }));
        vm.TotalFindingsCount = 1;
        vm.SessionStatus = "Completed";

        // Act
        await vm.ClearResultsAsync();

        // Assert
        Assert.Empty(vm.TargetInput);
        Assert.Empty(vm.SearchText);
        Assert.Empty(vm.Findings);
        Assert.Equal(0, vm.TotalFindingsCount);
        Assert.Equal(0, vm.DnsFindingsCount);
        Assert.Equal(0, vm.WhoisFindingsCount);
        Assert.Equal(0, vm.SubdomainFindingsCount);
        Assert.Equal(0, vm.HttpTechFindingsCount);
        Assert.Equal("Ready", vm.SessionStatus);
    }
}
