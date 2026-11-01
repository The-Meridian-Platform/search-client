using System;

namespace MeridianClient.Models;

public record LogEntry(
    DateTime Timestamp,
    ClientLogLevel Level,
    string Source,
    string Message,
    string? Details = null)
{
    public string TimestampDisplay => Timestamp.ToString("HH:mm:ss.fff");

    public string LevelBadgeText => Level switch
    {
        ClientLogLevel.Info => "INFO",
        ClientLogLevel.Warning => "WARN",
        ClientLogLevel.Error => "ERR",
        ClientLogLevel.Debug => "DBG",
        _ => "LOG"
    };

    public string LevelColor => Level switch
    {
        ClientLogLevel.Info => "#00D2FF",
        ClientLogLevel.Warning => "#FFB800",
        ClientLogLevel.Error => "#FF3366",
        ClientLogLevel.Debug => "#8E9AA8",
        _ => "#E2E8F0"
    };

    public string LevelBackground => Level switch
    {
        ClientLogLevel.Info => "#1A00D2FF",
        ClientLogLevel.Warning => "#1AFFB800",
        ClientLogLevel.Error => "#20FF3366",
        ClientLogLevel.Debug => "#158E9AA8",
        _ => "#15FFFFFF"
    };

    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

    public string FormattedLine =>
        $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{LevelBadgeText}] [{Source}] {Message}" +
        (HasDetails ? $"\n  Details: {Details}" : string.Empty);
}
