using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.CompilerServices;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Meridian.Client.Grpc;

namespace MeridianClient.Services;

public class MeridianStreamClient : IMeridianStreamClient
{
    private readonly ISettingsService _settingsService;
    private readonly ILogService _logService;

    public MeridianStreamClient(ISettingsService settingsService, ILogService logService)
    {
        _settingsService = settingsService;
        _logService = logService;
    }

    private GrpcChannel CreateChannel()
    {
        var backendUrl = _settingsService.Settings.BackendUrl.TrimEnd('/');

        if (_settingsService.Settings.AllowUnencryptedHttp2)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        var httpHandler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                // Accept self-signed / development certificates on localhost
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true
            },
            EnableMultipleHttp2Connections = true
        };

        // If backend URL is unencrypted HTTP (e.g. http://localhost:5168), Kestrel without TLS forces HTTP/1.1.
        // GrpcWebHandler enables streaming gRPC over HTTP/1.1 without HTTP_1_1_REQUIRED errors.
        HttpMessageHandler handler = httpHandler;
        var channelOptions = new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 32 * 1024 * 1024 // 32MB
        };

        if (backendUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            handler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, httpHandler);
            channelOptions.HttpVersion = new Version(1, 1);
            channelOptions.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        }

        channelOptions.HttpHandler = handler;
        return GrpcChannel.ForAddress(backendUrl, channelOptions);
    }

    public async IAsyncEnumerable<SearchStreamResponse> SubscribeToSessionAsync(
        string sessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var backendUrl = _settingsService.Settings.BackendUrl.TrimEnd('/');
        _logService.LogInfo("gRPC", $"Открытие gRPC стрима SubscribeToSession для сессии '{sessionId}' ({backendUrl})");

        using var channel = CreateChannel();
        var client = new SearchStreamService.SearchStreamServiceClient(channel);

        var request = new SubscribeSessionRequest
        {
            SessionId = sessionId
        };

        using var call = client.SubscribeToSession(request, cancellationToken: cancellationToken);

        IAsyncEnumerator<SearchStreamResponse>? enumerator = null;
        try
        {
            enumerator = call.ResponseStream.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception ex)
        {
            HandleGrpcException(sessionId, ex);
            throw;
        }

        var count = 0;
        while (true)
        {
            SearchStreamResponse item;
            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    break;
                }
                item = enumerator.Current;
            }
            catch (OperationCanceledException)
            {
                _logService.LogInfo("gRPC", $"Стрим для сессии '{sessionId}' отменен клиентом.");
                yield break;
            }
            catch (Exception ex)
            {
                HandleGrpcException(sessionId, ex);
                throw;
            }

            count++;
            switch (item.PayloadCase)
            {
                case SearchStreamResponse.PayloadOneofCase.SessionStarted:
                    _logService.LogInfo("gRPC", $"[#{count}] Сессия запущена на бэкенде: цель '{item.SessionStarted.TargetValue}'");
                    break;

                case SearchStreamResponse.PayloadOneofCase.FindingDiscovered:
                    var f = item.FindingDiscovered;
                    _logService.LogInfo("gRPC", $"[#{count}] Находка #{f.FindingId} [{f.Type}] от '{f.ProviderKey}' (размер json: {f.DataJson?.Length ?? 0} симв.)");
                    break;

                case SearchStreamResponse.PayloadOneofCase.SessionFinished:
                    var fin = item.SessionFinished;
                    _logService.LogInfo("gRPC", $"[#{count}] Сессия завершена бэкендом. Статус: '{fin.Status}', всего находок: {fin.TotalFindings}");
                    break;

                default:
                    _logService.LogDebug("gRPC", $"[#{count}] Получено служебное gRPC сообщение");
                    break;
            }

            yield return item;
        }

        _logService.LogInfo("gRPC", $"gRPC стрим для сессии '{sessionId}' успешно завершен. Всего событий: {count}");
    }

    private void HandleGrpcException(string sessionId, Exception ex)
    {
        var msg = ex.Message;
        if (msg.Contains("HTTP_1_1_REQUIRED", StringComparison.OrdinalIgnoreCase))
        {
            _logService.LogError("gRPC",
                $"gRPC сбой (HTTP_1_1_REQUIRED): Сервер на HTTP-порту ожидает HTTP/1.1 протокол. " +
                "Перезапустите backend MeridianHost.Api (где обновлена поддержка Http1AndHttp2) или переключите адрес в Настройках на HTTPS https://localhost:7013", ex);
        }
        else
        {
            _logService.LogError("gRPC", $"Ошибка gRPC стрима для сессии '{sessionId}': {msg}", ex);
        }
    }
}
