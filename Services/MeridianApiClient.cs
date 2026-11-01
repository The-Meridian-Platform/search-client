using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using MeridianClient.Models;

namespace MeridianClient.Services;

public class MeridianApiClient : IMeridianApiClient
{
    private readonly ISettingsService _settingsService;
    private readonly ILogService _logService;
    private readonly JsonSerializerOptions _jsonOptions;

    public MeridianApiClient(ISettingsService settingsService, ILogService logService)
    {
        _settingsService = settingsService;
        _logService = logService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private HttpClient CreateHttpClient(string? overrideUrl = null)
    {
        var handler = new HttpClientHandler
        {
            // Allow localhost development certificates
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri((overrideUrl ?? _settingsService.Settings.BackendUrl).TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(_settingsService.Settings.TimeoutSeconds)
        };

        return client;
    }

    public async Task<CreateSearchSessionResponse> CreateSessionAsync(
        string targetValue,
        string targetType = "Auto",
        CancellationToken cancellationToken = default)
    {
        using var client = CreateHttpClient();

        SearchTargetType? parsedType = null;
        if (!string.Equals(targetType, "Auto", StringComparison.OrdinalIgnoreCase) &&
            Enum.TryParse<SearchTargetType>(targetType, true, out var enumType))
        {
            parsedType = enumType;
        }

        var requestBody = new CreateSearchSessionRequest
        {
            Value = targetValue.Trim(),
            TargetType = parsedType
        };

        _logService.LogInfo("REST", $"POST /api/search-sessions -> Значение: '{requestBody.Value}', Тип: '{requestBody.TargetType?.ToString() ?? "Auto"}'");

        var response = await client.PostAsJsonAsync("/api/search-sessions", requestBody, _jsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logService.LogError("REST", $"POST /api/search-sessions завершился с ошибкой {(int)response.StatusCode} {response.ReasonPhrase}", new HttpRequestException(errorContent));
            throw new HttpRequestException($"{(int)response.StatusCode} ({response.ReasonPhrase}): {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<CreateSearchSessionResponse>(_jsonOptions, cancellationToken);
        if (result == null || string.IsNullOrWhiteSpace(result.EffectiveId))
        {
            _logService.LogError("REST", "Сервер вернул пустой идентификатор сессии.");
            throw new InvalidOperationException("Backend returned an empty session ID.");
        }

        _logService.LogInfo("REST", $"Сессия поиска создана успешно: ID={result.EffectiveId}");
        return result;
    }

    public async Task<List<SearchSessionModel>> GetSessionsHistoryAsync(CancellationToken cancellationToken = default)
    {
        using var client = CreateHttpClient();

        var response = await client.GetAsync("/api/search-sessions?includeFindings=false", cancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<List<SearchSessionModel>>(_jsonOptions, cancellationToken);
        return list ?? new List<SearchSessionModel>();
    }

    public async Task<SearchSessionModel?> GetSessionDetailsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using var client = CreateHttpClient();

        var response = await client.GetAsync($"/api/search-sessions/{sessionId}?includeFindings=true", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SearchSessionModel>(_jsonOptions, cancellationToken);
    }

    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _logService.LogInfo("REST", $"DELETE /api/search-sessions/{sessionId}...");
        try
        {
            using var client = CreateHttpClient();
            var response = await client.DeleteAsync($"/api/search-sessions/{sessionId}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logService.LogInfo("REST", $"Сессия {sessionId} успешно удалена с бэкенда (HTTP {(int)response.StatusCode}).");
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logService.LogWarning("REST", $"Сервер отклонил удаление сессии {sessionId}: {(int)response.StatusCode} {errorContent}");
            return false;
        }
        catch (Exception ex)
        {
            _logService.LogError("REST", $"Ошибка при выполнении DELETE /api/search-sessions/{sessionId}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(string? customUrl = null, CancellationToken cancellationToken = default)
    {
        var targetUrl = customUrl ?? _settingsService.Settings.BackendUrl;
        _logService.LogInfo("REST", $"Проверка REST API соединения: {targetUrl}...");
        try
        {
            using var client = CreateHttpClient(customUrl);
            var response = await client.GetAsync("/api/search-sessions?includeFindings=false", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var msg = $"Успешное подключение к {targetUrl}. Код: {(int)response.StatusCode} {response.StatusCode}";
                _logService.LogInfo("REST", msg);
                return (true, msg);
            }
            var failMsg = $"Сервер вернул статус: {(int)response.StatusCode} {response.ReasonPhrase}";
            _logService.LogWarning("REST", failMsg);
            return (false, failMsg);
        }
        catch (Exception ex)
        {
            var err = $"Ошибка подключения: {ex.Message}";
            _logService.LogError("REST", err, ex);
            return (false, err);
        }
    }
}
