using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeridianClient.Models;
using MeridianClient.Services;

namespace MeridianClient.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IMeridianApiClient _apiClient;

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

    public ObservableCollection<SearchSessionModel> Sessions { get; } = new();

    public bool HasSessions => Sessions.Count > 0;
    public bool IsEmpty => Sessions.Count == 0 && !IsLoading;

    public event Action<SearchSessionModel>? SessionDetailsLoaded;

    public HistoryViewModel(IMeridianApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var sessions = await _apiClient.GetSessionsHistoryAsync();
            Sessions.Clear();
            foreach (var session in sessions.OrderByDescending(s => s.CreatedAt ?? s.StartedAt))
            {
                Sessions.Add(session);
            }
            OnPropertyChanged(nameof(HasSessions));
            OnPropertyChanged(nameof(IsEmpty));
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
            }
            else
            {
                ErrorMessage = "Сервер отклонил удаление сессии.";
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
