using System.ComponentModel;

namespace MeridianClient.Services;

public interface IHealthCheckService : INotifyPropertyChanged
{
    bool IsHealthy { get; }
    bool IsChecking { get; }
    string StatusText { get; }
    long LatencyMs { get; }
    string ErrorMessage { get; }
    DateTime? LastChecked { get; }

    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
    void StartPeriodicChecking(TimeSpan interval);
    void StopPeriodicChecking();
}
