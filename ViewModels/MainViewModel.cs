using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeridianClient.Services;

namespace MeridianClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public SearchViewModel Search { get; }
    public GraphViewModel Graph { get; }
    public HistoryViewModel History { get; }
    public LogsViewModel Logs { get; }
    public SettingsViewModel Settings { get; }
    public IHealthCheckService Health { get; }

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    [ObservableProperty]
    private string _currentTab = "Search"; // Search, Graph, History, Logs, Settings

    public MainViewModel(
        SearchViewModel searchViewModel,
        GraphViewModel graphViewModel,
        HistoryViewModel historyViewModel,
        LogsViewModel logsViewModel,
        SettingsViewModel settingsViewModel,
        IHealthCheckService healthCheckService)
    {
        Search = searchViewModel;
        Graph = graphViewModel;
        History = historyViewModel;
        Logs = logsViewModel;
        Settings = settingsViewModel;
        Health = healthCheckService;

        _currentViewModel = Search;

        History.SessionDetailsLoaded += session =>
        {
            Search.LoadExistingSession(session);
            Graph.SelectedSession = session;
            _ = Graph.LoadSessionByIdAsync(session.EffectiveId, session.EffectiveTarget);
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
            "Graph" => Graph,
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
        else if (destination == "Graph")
        {
            _ = History.LoadHistoryAsync();
            if (Graph.IsLiveMode)
            {
                if (Graph.Nodes.Count == 0 && Search.Findings.Count > 0)
                {
                    Graph.SyncFromLiveSearch();
                }
            }
        }
    }
}
