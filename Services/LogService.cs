using System;
using System.Collections.Generic;
using MeridianClient.Models;

namespace MeridianClient.Services;

public class LogService : ILogService
{
    private const int MaxEntries = 2000;
    private readonly List<LogEntry> _logs = new();
    private readonly object _lock = new();

    private int _infoCount;
    private int _warningCount;
    private int _errorCount;
    private int _debugCount;

    public event Action<LogEntry>? LogAdded;
    public event Action? LogsCleared;

    public int TotalCount
    {
        get
        {
            lock (_lock) return _logs.Count;
        }
    }

    public int InfoCount
    {
        get
        {
            lock (_lock) return _infoCount;
        }
    }

    public int WarningCount
    {
        get
        {
            lock (_lock) return _warningCount;
        }
    }

    public int ErrorCount
    {
        get
        {
            lock (_lock) return _errorCount;
        }
    }

    public int DebugCount
    {
        get
        {
            lock (_lock) return _debugCount;
        }
    }

    public void Log(ClientLogLevel level, string source, string message, string? details = null)
    {
        var entry = new LogEntry(
            Timestamp: DateTime.Now,
            Level: level,
            Source: source,
            Message: message,
            Details: details
        );

        lock (_lock)
        {
            if (_logs.Count >= MaxEntries)
            {
                var removed = _logs[0];
                _logs.RemoveAt(0);
                DecrementCount(removed.Level);
            }

            _logs.Add(entry);
            IncrementCount(level);
        }

        try
        {
            LogAdded?.Invoke(entry);
        }
        catch
        {
            // Do not break execution on logging observer exceptions
        }
    }

    public void LogInfo(string source, string message, string? details = null) =>
        Log(ClientLogLevel.Info, source, message, details);

    public void LogWarning(string source, string message, string? details = null) =>
        Log(ClientLogLevel.Warning, source, message, details);

    public void LogError(string source, string message, Exception? ex = null)
    {
        var details = ex != null
            ? $"{ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}"
            : null;
        Log(ClientLogLevel.Error, source, message, details);
    }

    public void LogDebug(string source, string message, string? details = null) =>
        Log(ClientLogLevel.Debug, source, message, details);

    public IReadOnlyList<LogEntry> GetAllLogs()
    {
        lock (_lock)
        {
            return _logs.ToArray();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _logs.Clear();
            _infoCount = 0;
            _warningCount = 0;
            _errorCount = 0;
            _debugCount = 0;
        }

        try
        {
            LogsCleared?.Invoke();
        }
        catch
        {
            // Ignore
        }
    }

    private void IncrementCount(ClientLogLevel level)
    {
        switch (level)
        {
            case ClientLogLevel.Info:
                _infoCount++;
                break;
            case ClientLogLevel.Warning:
                _warningCount++;
                break;
            case ClientLogLevel.Error:
                _errorCount++;
                break;
            case ClientLogLevel.Debug:
                _debugCount++;
                break;
        }
    }

    private void DecrementCount(ClientLogLevel level)
    {
        switch (level)
        {
            case ClientLogLevel.Info:
                if (_infoCount > 0) _infoCount--;
                break;
            case ClientLogLevel.Warning:
                if (_warningCount > 0) _warningCount--;
                break;
            case ClientLogLevel.Error:
                if (_errorCount > 0) _errorCount--;
                break;
            case ClientLogLevel.Debug:
                if (_debugCount > 0) _debugCount--;
                break;
        }
    }
}
