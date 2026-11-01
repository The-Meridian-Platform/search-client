using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeridianClient.Models;
using MeridianClient.Services;

namespace MeridianClient.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly ILogService _logService;
    private const int MaxDisplayedLogs = 1000;

    public ObservableCollection<LogEntry> FilteredLogs { get; } = new();

    public event Action? RequestScrollToEnd;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private string _selectedLevel = "All"; // All, Info, Warning, Error, Debug

    [ObservableProperty]
    private string _selectedSource = "All"; // All, REST, gRPC, Health, Search, System

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private bool _isFiltersPanelOpen = true;

    [ObservableProperty]
    private string _statusNotification = string.Empty;

    public int TotalCount => _logService.TotalCount;
    public int InfoCount => _logService.InfoCount;
    public int WarningCount => _logService.WarningCount;
    public int ErrorCount => _logService.ErrorCount;
    public int DebugCount => _logService.DebugCount;

    public ObservableCollection<string> LevelFilters { get; } = new()
    {
        "All", "Info", "Warning", "Error", "Debug"
    };

    public ObservableCollection<string> SourceFilters { get; } = new()
    {
        "All", "REST", "gRPC", "HEALTH", "Search", "UI", "System"
    };

    public LogsViewModel(ILogService logService)
    {
        _logService = logService;

        _logService.LogAdded += OnLogAdded;
        _logService.LogsCleared += OnLogsCleared;

        // Initial load
        RefreshFilteredList();

        _logService.LogInfo("System", "Система логирования инициализирована. Запущен журнал событий Cyber OSINT.");
    }

    partial void OnFilterTextChanged(string value) => RefreshFilteredList();
    partial void OnSelectedLevelChanged(string value) => RefreshFilteredList();
    partial void OnSelectedSourceChanged(string value) => RefreshFilteredList();

    private void OnLogAdded(LogEntry entry)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null) return;

        dispatcher.BeginInvoke(() =>
        {
            NotifyCountProperties();

            if (MatchesFilter(entry))
            {
                if (FilteredLogs.Count >= MaxDisplayedLogs)
                {
                    FilteredLogs.RemoveAt(0);
                }

                FilteredLogs.Add(entry);

                if (AutoScroll)
                {
                    RequestScrollToEnd?.Invoke();
                }
            }
        });
    }

    private void OnLogsCleared()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null) return;

        dispatcher.BeginInvoke(() =>
        {
            FilteredLogs.Clear();
            NotifyCountProperties();
            StatusNotification = "Журнал очищен";
        });
    }

    private void NotifyCountProperties()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(InfoCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(DebugCount));
    }

    public void RefreshFilteredList()
    {
        var all = _logService.GetAllLogs();
        var filtered = all.Where(MatchesFilter).TakeLast(MaxDisplayedLogs).ToList();

        FilteredLogs.Clear();
        foreach (var entry in filtered)
        {
            FilteredLogs.Add(entry);
        }

        NotifyCountProperties();

        if (AutoScroll && FilteredLogs.Count > 0)
        {
            RequestScrollToEnd?.Invoke();
        }
    }

    private bool MatchesFilter(LogEntry entry)
    {
        // Level match
        if (SelectedLevel != "All")
        {
            if (!string.Equals(entry.Level.ToString(), SelectedLevel, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Source match
        if (SelectedSource != "All")
        {
            if (!string.Equals(entry.Source, SelectedSource, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Text query
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            var query = FilterText.Trim();
            var matchesMsg = entry.Message.Contains(query, StringComparison.OrdinalIgnoreCase);
            var matchesSrc = entry.Source.Contains(query, StringComparison.OrdinalIgnoreCase);
            var matchesDetails = entry.Details != null && entry.Details.Contains(query, StringComparison.OrdinalIgnoreCase);

            if (!matchesMsg && !matchesSrc && !matchesDetails)
            {
                return false;
            }
        }

        return true;
    }

    [RelayCommand]
    public void SetLevelFilter(string level)
    {
        SelectedLevel = level;
    }

    [RelayCommand]
    public void ClearLogs()
    {
        _logService.Clear();
    }

    [RelayCommand]
    public void CopyAllLogs()
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Meridian OSINT Platform Logs Export - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"# Total: {FilteredLogs.Count} entries");
            sb.AppendLine(new string('-', 80));

            foreach (var log in FilteredLogs)
            {
                sb.AppendLine(log.FormattedLine);
            }

            Clipboard.SetText(sb.ToString());
            StatusNotification = $"Скопировано {FilteredLogs.Count} записей в буфер обмена";
        }
        catch (Exception ex)
        {
            StatusNotification = $"Ошибка копирования: {ex.Message}";
        }
    }

    [RelayCommand]
    public void CopyLogEntry(LogEntry? entry)
    {
        if (entry == null) return;

        try
        {
            Clipboard.SetText(entry.FormattedLine);
            StatusNotification = $"Запись [{entry.LevelBadgeText}] скопирована в буфер";
        }
        catch (Exception ex)
        {
            StatusNotification = $"Ошибка копирования: {ex.Message}";
        }
    }
}
