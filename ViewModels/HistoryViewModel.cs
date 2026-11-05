using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeridianClient.Models;
using MeridianClient.Services;

namespace MeridianClient.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IMeridianApiClient _apiClient;
    private readonly IToastService _toastService;

    [ObservableProperty]
    private bool _isLoading;

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsEmpty));
    }

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private SearchSessionModel? _selectedSession;

    [ObservableProperty]
    private bool _filterByCurrentInvestigation = true;

    [ObservableProperty]
    private string _currentInvestigationTitle = string.Empty;

    public List<string> CurrentInvestigationSessionIds { get; } = new();

    private readonly List<SearchSessionModel> _allRawSessions = new();

    public ObservableCollection<SearchSessionModel> Sessions { get; } = new();

    public bool HasSessions => Sessions.Count > 0;
    public bool IsEmpty => Sessions.Count == 0 && !IsLoading;

    public event Action<SearchSessionModel>? SessionDetailsLoaded;

    public HistoryViewModel(IMeridianApiClient apiClient, IToastService? toastService = null)
    {
        _apiClient = apiClient;
        _toastService = toastService ?? new ToastService(new SettingsService());
    }

    public void SetCurrentInvestigation(InvestigationModel? inv)
    {
        CurrentInvestigationSessionIds.Clear();
        if (inv != null)
        {
            CurrentInvestigationTitle = inv.Title;
            CurrentInvestigationSessionIds.AddRange(inv.SessionIds);
        }
        else
        {
            CurrentInvestigationTitle = string.Empty;
        }

        ApplyFilter();
    }

    partial void OnFilterByCurrentInvestigationChanged(bool value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var selectedId = SelectedSession?.EffectiveId;
        Sessions.Clear();

        var source = _allRawSessions.AsEnumerable();

        if (FilterByCurrentInvestigation && CurrentInvestigationSessionIds.Count > 0)
        {
            source = source.Where(s => CurrentInvestigationSessionIds.Contains(s.EffectiveId));
        }

        foreach (var s in source)
        {
            Sessions.Add(s);
        }

        if (!string.IsNullOrEmpty(selectedId))
        {
            SelectedSession = Sessions.FirstOrDefault(s => s.EffectiveId == selectedId);
        }

        OnPropertyChanged(nameof(HasSessions));
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var sessions = await _apiClient.GetSessionsHistoryAsync();
            _allRawSessions.Clear();
            foreach (var session in sessions.OrderByDescending(s => s.CreatedAt ?? s.StartedAt))
            {
                _allRawSessions.Add(session);
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить историю: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasSessions));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    public async Task OpenSessionAsync(SearchSessionModel? session)
    {
        session ??= SelectedSession;
        if (session == null || string.IsNullOrWhiteSpace(session.EffectiveId))
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var fullSession = await _apiClient.GetSessionDetailsAsync(session.EffectiveId);
            if (fullSession != null)
            {
                SessionDetailsLoaded?.Invoke(fullSession);
                _toastService.ShowSuccess("Сессия открыта", $"Сессия «{session.EffectiveTarget}» загружена.");
            }
            else
            {
                ErrorMessage = "Сессия не найдена на сервере.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки находок сессии: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteSessionAsync(SearchSessionModel? session)
    {
        session ??= SelectedSession;
        if (session == null || string.IsNullOrWhiteSpace(session.EffectiveId))
            return;

        var sessionId = session.EffectiveId;
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var success = await _apiClient.DeleteSessionAsync(sessionId);
            if (success)
            {
                Sessions.Remove(session);
                if (SelectedSession?.EffectiveId == sessionId)
                {
                    SelectedSession = null;
                }
                OnPropertyChanged(nameof(HasSessions));
                OnPropertyChanged(nameof(IsEmpty));
                _toastService.ShowSuccess("Сессия удалена", $"Сессия «{session.EffectiveTarget}» удалена.");
            }
            else
            {
                ErrorMessage = "Сервер отклонил удаление сессии.";
                _toastService.ShowError("Ошибка удаления", "Сервер отклонил удаление сессии.");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка при удалении сессии: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
