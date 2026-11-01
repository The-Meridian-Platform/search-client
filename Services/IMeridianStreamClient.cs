using Meridian.Client.Grpc;

namespace MeridianClient.Services;

public interface IMeridianStreamClient
{
    IAsyncEnumerable<SearchStreamResponse> SubscribeToSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
