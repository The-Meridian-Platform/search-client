using System;
using System.Collections.Generic;
using MeridianClient.Models;

namespace MeridianClient.Services;

public interface ILogService
{
    event Action<LogEntry>? LogAdded;
    event Action? LogsCleared;

    void Log(ClientLogLevel level, string source, string message, string? details = null);
    void LogInfo(string source, string message, string? details = null);
    void LogWarning(string source, string message, string? details = null);
    void LogError(string source, string message, Exception? ex = null);
    void LogDebug(string source, string message, string? details = null);

    IReadOnlyList<LogEntry> GetAllLogs();
    void Clear();

    int TotalCount { get; }
    int InfoCount { get; }
    int WarningCount { get; }
    int ErrorCount { get; }
    int DebugCount { get; }
}
