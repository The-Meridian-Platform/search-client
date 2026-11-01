using MeridianClient.Models;

namespace MeridianClient.Services;

public interface IMeridianApiClient
{
    Task<CreateSearchSessionResponse> CreateSessionAsync(string targetValue, string targetType = "Auto", CancellationToken cancellationToken = default);
    Task<List<SearchSessionModel>> GetSessionsHistoryAsync(CancellationToken cancellationToken = default);
    Task<SearchSessionModel?> GetSessionDetailsAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> TestConnectionAsync(string? customUrl = null, CancellationToken cancellationToken = default);
}
