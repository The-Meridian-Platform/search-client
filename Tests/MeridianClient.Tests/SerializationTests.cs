using System.Text.Json;
using MeridianClient.Models;

namespace MeridianClient.Tests;

public class SerializationTests
{
    [Fact]
    public void CreateSearchSessionResponse_DeserializesSearchSessionId()
    {
        var json = """{"searchSessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"}""";
        var result = JsonSerializer.Deserialize<CreateSearchSessionResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("3fa85f64-5717-4562-b3fc-2c963f66afa6", result.EffectiveId);
    }

    [Fact]
    public void SearchSessionModel_DeserializesBackendFormatWithFinding()
    {
        var json = """
        {
            "id": "c7606e1b-3f04-4e2a-a957-d64e9a6e1189",
            "target": {
                "id": "e85789f2-2211-4777-9099-312932847120",
                "value": "ayanami-connect.online",
                "targetType": "Domain"
            },
            "finding": [
                {
                    "id": "90327fbc-1122-3344-5566-778899aabbcc",
                    "providerKey": "DnsProvider",
                    "type": "A",
                    "dataJson": "{\"value\":\"1.2.3.4\"}",
                    "createdAt": "2026-10-31T20:00:00Z"
                }
            ],
            "status": "Completed",
            "createdAt": "2026-10-31T20:00:00Z",
            "startedAt": "2026-10-31T20:00:01Z",
            "finishedAt": "2026-10-31T20:00:05Z"
        }
        """;

        var session = JsonSerializer.Deserialize<SearchSessionModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(session);
        Assert.Equal("c7606e1b-3f04-4e2a-a957-d64e9a6e1189", session.EffectiveId);
        Assert.Equal("ayanami-connect.online", session.EffectiveTarget);
        Assert.Equal("Completed", session.Status);
        Assert.Single(session.Findings);
        Assert.Equal(1, session.TotalFindings);
        Assert.Equal("A", session.Findings[0].Type);
    }
}
