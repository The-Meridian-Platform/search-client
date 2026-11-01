using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MeridianClient.Services;

public partial class HealthCheckService : ObservableObject, IHealthCheckService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogService _logService;
    private DispatcherTimer? _timer;
    private readonly HttpClient _httpClient;
    private bool? _lastReportedHealthy;

    [ObservableProperty]
    private bool _isHealthy;

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private string _statusText = "CHECKING...";

    [ObservableProperty]
    private long _latencyMs;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private DateTime? _lastChecked;

    public HealthCheckService(ISettingsService settingsService, ILogService logService)
    {
        _settingsService = settingsService;
        _logService = logService;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(3)
        };

        _settingsService.SettingsChanged += () =>
        {
            _lastReportedHealthy = null;
            _ = CheckHealthAsync();
        };

        StartPeriodicChecking(TimeSpan.FromSeconds(4));
    }

    public void StartPeriodicChecking(TimeSpan interval)
    {
        _timer?.Stop();
        _timer = new DispatcherTimer
        {
            Interval = interval
        };
        _timer.Tick += async (s, e) => await CheckHealthAsync();
        _timer.Start();

        // Initial check immediately
        _ = CheckHealthAsync();
    }

    public void StopPeriodicChecking()
    {
        _timer?.Stop();
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (IsChecking)
            return IsHealthy;

        IsChecking = true;
        var sw = Stopwatch.StartNew();

        try
        {
            var baseUrl = _settingsService.Settings.BackendUrl.TrimEnd('/');
            var healthUrl = $"{baseUrl}/health";

            using var response = await _httpClient.GetAsync(healthUrl, cancellationToken);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                IsHealthy = true;
                LatencyMs = sw.ElapsedMilliseconds;
                StatusText = $"ONLINE ({LatencyMs}ms)";
                ErrorMessage = string.Empty;

                if (_lastReportedHealthy != true)
                {
                    _logService.LogInfo("HEALTH", $"Соединение с бэкендом стабильно: {healthUrl} (пинг: {LatencyMs}мс)");
                    _lastReportedHealthy = true;
                }
            }
            else
            {
                // Fallback attempt to search-sessions endpoint
                var fallbackUrl = $"{baseUrl}/api/search-sessions?includeFindings=false";
                using var fallbackResp = await _httpClient.GetAsync(fallbackUrl, cancellationToken);
                sw.Stop();

                if (fallbackResp.IsSuccessStatusCode)
                {
                    IsHealthy = true;
                    LatencyMs = sw.ElapsedMilliseconds;
                    StatusText = $"ONLINE ({LatencyMs}ms)";
                    ErrorMessage = string.Empty;

                    if (_lastReportedHealthy != true)
                    {
                        _logService.LogInfo("HEALTH", $"Соединение с бэкендом доступно через fallback: {fallbackUrl} (пинг: {LatencyMs}мс)");
                        _lastReportedHealthy = true;
                    }
                }
                else
                {
                    IsHealthy = false;
                    LatencyMs = sw.ElapsedMilliseconds;
                    StatusText = $"HTTP {(int)response.StatusCode}";
                    ErrorMessage = $"Сервер вернул статус: {response.StatusCode}";

                    if (_lastReportedHealthy != false)
                    {
                        _logService.LogWarning("HEALTH", $"Проверка /health вернула статус HTTP {(int)response.StatusCode}");
                        _lastReportedHealthy = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            IsHealthy = false;
            LatencyMs = 0;
            StatusText = "OFFLINE";
            ErrorMessage = ex.Message;

            if (_lastReportedHealthy != false)
            {
                _logService.LogWarning("HEALTH", $"Сервер недоступен ({_settingsService.Settings.BackendUrl}): {ex.Message}");
                _lastReportedHealthy = false;
            }
        }
        finally
        {
            LastChecked = DateTime.Now;
            IsChecking = false;
        }

        return IsHealthy;
    }
}
