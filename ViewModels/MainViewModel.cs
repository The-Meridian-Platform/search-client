using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeridianClient.Services;

namespace MeridianClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public SearchViewModel Search { get; }
    public HistoryViewModel History { get; }
    public LogsViewModel Logs { get; }
    public SettingsViewModel Settings { get; }
    public IHealthCheckService Health { get; }

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    [ObservableProperty]
    private string _currentTab = "Search"; // Search, History, Logs, Settings

    public MainViewModel(
        SearchViewModel searchViewModel,
        HistoryViewModel historyViewModel,
        LogsViewModel logsViewModel,
        SettingsViewModel settingsViewModel,
        IHealthCheckService healthCheckService)
    {
        Search = searchViewModel;
        History = historyViewModel;
        Logs = logsViewModel;
        Settings = settingsViewModel;
        Health = healthCheckService;

        _currentViewModel = Search;

        History.SessionDetailsLoaded += session =>
        {
            Search.LoadExistingSession(session);
            NavigateTo("Search");
        };
    }

    [RelayCommand]
    public async Task CheckHealthAsync()
    {
        await Health.CheckHealthAsync();
    }

    [RelayCommand]
    public void NavigateTo(string destination)
    {
        CurrentTab = destination;
        CurrentViewModel = destination switch
        {
            "Search" => Search,
            "History" => History,
            "Logs" => Logs,
            "Settings" => Settings,
            _ => Search
        };

        if (destination == "History")
        {
            _ = History.LoadHistoryAsync();
        }
        else if (destination == "Logs")
        {
            Logs.RefreshFilteredList();
        }
    }
}
