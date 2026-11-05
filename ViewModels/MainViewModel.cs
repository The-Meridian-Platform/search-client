using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeridianClient.Services;

namespace MeridianClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public WelcomeViewModel Welcome { get; }
    public SearchViewModel Search { get; }
    public GraphViewModel Graph { get; }
    public HistoryViewModel History { get; }
    public LogsViewModel Logs { get; }
    public SettingsViewModel Settings { get; }
    public IHealthCheckService Health { get; }

    public event Action? ExitToHubRequested;

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    [ObservableProperty]
    private string _currentTab = "Welcome";

    public MainViewModel(
        WelcomeViewModel welcomeViewModel,
        SearchViewModel searchViewModel,
        GraphViewModel graphViewModel,
        HistoryViewModel historyViewModel,
        LogsViewModel logsViewModel,
        SettingsViewModel settingsViewModel,
        IHealthCheckService healthCheckService,
        ISettingsService settingsService)
    {
        Welcome = welcomeViewModel;
        Search = searchViewModel;
        Graph = graphViewModel;
        History = historyViewModel;
        Logs = logsViewModel;
        Settings = settingsViewModel;
        Health = healthCheckService;

        _currentViewModel = settingsService.Settings.ShowWelcomeScreenOnStartup ? Welcome : Search;
        _currentTab = settingsService.Settings.ShowWelcomeScreenOnStartup ? "Welcome" : "Search";

        Welcome.NavigateToSearchRequested += () => NavigateTo("Search");
        Welcome.NavigateToGraphRequested += () => NavigateTo("Graph");
        Welcome.OpenInvestigationRequested += inv =>
        {
            Graph.SelectedInvestigation = inv;
            NavigateTo("Graph");
        };

        Graph.ExitToHubRequested += () => ExitToHubRequested?.Invoke();

        History.SessionDetailsLoaded += session =>
        {
            Search.LoadExistingSession(session);
            Graph.SelectedSession = session;
            _ = Graph.LoadSessionByIdAsync(session.EffectiveId, session.EffectiveTarget);
            NavigateTo("Search");
        };
    }

    [RelayCommand]
    public void ExitToHub()
    {
        ExitToHubRequested?.Invoke();
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
            "Welcome" => Welcome,
            "Search" => Search,
            "Graph" => Graph,
            "History" => History,
            "Logs" => Logs,
            "Settings" => Settings,
            _ => Welcome
        };

        if (destination == "Welcome")
        {
            Welcome.RefreshRecentInvestigations();
        }
        else if (destination == "History")
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
