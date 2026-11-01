using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grpc.Core;
using Meridian.Client.Grpc;
using MeridianClient.Models;
using MeridianClient.Services;

namespace MeridianClient.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly IMeridianApiClient _apiClient;
    private readonly IMeridianStreamClient _streamClient;
    private readonly ILogService _logService;
    private readonly object _findingsLock = new();

    private CancellationTokenSource? _searchCts;
    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _timer;
    private int _searchSessionGeneration;

    [ObservableProperty]
    private string _targetInput = "example.com";

    [ObservableProperty]
    private string _activeTarget = string.Empty;

    [ObservableProperty]
    private string _selectedTargetType = "Auto";

    public ObservableCollection<string> TargetTypes { get; } = new()
    {
        "Auto",
        "Domain",
        "IpAddress",
        "Email",
        "Username"
    };

    [ObservableProperty]
    private string _currentSessionId = string.Empty;

    [ObservableProperty]
    private string _sessionStatus = "Ready"; // Ready, Pending, Running, Completed, Failed

    [ObservableProperty]
    private string _statusMessage = "Готов к запуску поиска";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _elapsedTime = "00:00.000";

    [ObservableProperty]
    private int _totalFindingsCount;

    [ObservableProperty]
    private int _dnsFindingsCount;

    [ObservableProperty]
    private int _whoisFindingsCount;

    [ObservableProperty]
    private int _subdomainFindingsCount;

    [ObservableProperty]
    private int _httpTechFindingsCount;

    [ObservableProperty]
    private string _activeFilter = "All"; // All, DNS, Whois, Subdomains, Tech, Other

    [ObservableProperty]
    private bool _isFiltersPanelOpen;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<FindingItemViewModel> Findings { get; } = new();

    private ICollectionView? _findingsView;
    public ICollectionView FindingsView
    {
        get
        {
            if (_findingsView == null)
            {
                _findingsView = CollectionViewSource.GetDefaultView(Findings);
                _findingsView.Filter = FilterFindingItem;
            }
            return _findingsView;
        }
    }

    public SearchViewModel(IMeridianApiClient apiClient, IMeridianStreamClient streamClient, ILogService logService)
    {
        _apiClient = apiClient;
        _streamClient = streamClient;
        _logService = logService;

        BindingOperations.EnableCollectionSynchronization(Findings, _findingsLock);

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _timer.Tick += (s, e) =>
        {
            var ts = _stopwatch.Elapsed;
            ElapsedTime = $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        };
    }

    [RelayCommand]
    public async Task StartSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetInput))
        {
            StatusMessage = "Введите целевой домен или IP";
            return;
        }

        CancelCurrentSearch();
        _searchSessionGeneration++;
        int currentGeneration = _searchSessionGeneration;

        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        ActiveTarget = TargetInput.Trim();

        lock (_findingsLock)
        {
            Findings.Clear();
        }
        TotalFindingsCount = 0;
        DnsFindingsCount = 0;
        WhoisFindingsCount = 0;
        SubdomainFindingsCount = 0;
        HttpTechFindingsCount = 0;
        ElapsedTime = "00:00.000";

        SessionStatus = "Pending";
        IsRunning = true;
        StatusMessage = "Инициализация сессии через REST API...";
        _stopwatch.Restart();
        _timer.Start();

        _logService.LogInfo("Search", $"Запуск поиска: цель='{TargetInput}', тип='{SelectedTargetType}'");

        try
        {
            // 1. Create Session via POST /api/search-sessions
            var sessionResponse = await _apiClient.CreateSessionAsync(TargetInput.Trim(), SelectedTargetType, ct);
            CurrentSessionId = sessionResponse.EffectiveId;
            StatusMessage = $"Сессия создана [{CurrentSessionId}]. Подключение к gRPC стриму...";
            _logService.LogInfo("Search", $"Сессия [{CurrentSessionId}] создана. Подключение к gRPC...");

            // 2. Open gRPC Stream and receive real-time updates
            var stream = _streamClient.SubscribeToSessionAsync(CurrentSessionId, ct);

            await foreach (var message in stream.WithCancellation(ct))
            {
                HandleStreamMessage(message);
            }

            if (SessionStatus == "Running" || SessionStatus == "Pending")
            {
                SessionStatus = "Completed";
                StatusMessage = $"Поиск завершен. Найдено записей: {TotalFindingsCount}";
                _logService.LogInfo("Search", $"Поиск успешно завершен. Всего находок: {TotalFindingsCount} (DNS: {DnsFindingsCount}, WHOIS: {WhoisFindingsCount}) за {ElapsedTime}");
            }
        }
        catch (OperationCanceledException)
        {
            SessionStatus = "Cancelled";
            StatusMessage = "Поиск остановлен пользователем";
            _logService.LogWarning("Search", "Поиск отменен пользователем");
        }
        catch (RpcException ex)
        {
            SessionStatus = "Failed";
            StatusMessage = $"Ошибка gRPC [{ex.StatusCode}]: {ex.Status.Detail}";
            _logService.LogError("Search", $"Сбой gRPC [{ex.StatusCode}]: {ex.Status.Detail}", ex);
        }
        catch (Exception ex)
        {
            SessionStatus = "Failed";
            StatusMessage = $"Ошибка: {ex.Message}";
            _logService.LogError("Search", $"Сбой поиска: {ex.Message}", ex);
        }
        finally
        {
            _stopwatch.Stop();
            _timer.Stop();
            IsRunning = false;
        }
    }

    [RelayCommand]
    public void StopSearch()
    {
        _logService.LogInfo("Search", "Пользователь нажал 'Остановить'");
        CancelCurrentSearch();
        _searchSessionGeneration++;
        IsRunning = false;
        _stopwatch.Stop();
        _timer.Stop();
        if (SessionStatus == "Running" || SessionStatus == "Pending")
        {
            SessionStatus = "Cancelled";
            StatusMessage = "Поиск остановлен";
        }
    }

    [RelayCommand]
    public async Task ClearResultsAsync()
    {
        _logService.LogInfo("Search", "Очистка текущих результатов поиска и полей ввода.");
        if (IsRunning)
        {
            StopSearch();
        }

        ActiveTarget = string.Empty;
        _searchSessionGeneration++;

        // 1. Fast sequential "fly away" animation for finding items
        List<FindingItemViewModel> snapshot;
        lock (_findingsLock)
        {
            snapshot = Findings.ToList();
        }

        if (snapshot.Count > 0)
        {
            // Calculate delay per item so total fly-away duration is brisk (between 120ms and 300ms max)
            int itemDelay = Math.Clamp(220 / Math.Max(snapshot.Count, 1), 6, 25);
            foreach (var item in snapshot)
            {
                item.IsDismissing = true;
                if (itemDelay > 0)
                {
                    await Task.Delay(itemDelay);
                }
            }

            // Brief wait for the CSS/Storyboard fly-away animation (180ms) of the last items to complete
            await Task.Delay(140);
        }

        // 2. Clear findings
        lock (_findingsLock)
        {
            Findings.Clear();
        }
        TotalFindingsCount = 0;
        DnsFindingsCount = 0;
        WhoisFindingsCount = 0;
        SubdomainFindingsCount = 0;
        HttpTechFindingsCount = 0;
        ElapsedTime = "00:00.000";
        SessionStatus = "Ready";
        StatusMessage = "Готов к запуску поиска";
        CurrentSessionId = string.Empty;

        // 3. Fast animated text backspace erasing in input fields
        await EraseInputFieldsAnimatedAsync();

        SafeRefreshFindingsView();
    }

    private async Task EraseInputFieldsAnimatedAsync()
    {
        var targetText = TargetInput;
        var searchTxt = SearchText;

        if (string.IsNullOrEmpty(targetText) && string.IsNullOrEmpty(searchTxt))
            return;

        int maxLen = Math.Max(targetText.Length, searchTxt.Length);
        // Total erasing duration between 120ms and 240ms
        int stepDelay = Math.Clamp(180 / Math.Max(maxLen, 1), 10, 30);

        while (!string.IsNullOrEmpty(TargetInput) || !string.IsNullOrEmpty(SearchText))
        {
            if (!string.IsNullOrEmpty(TargetInput))
            {
                TargetInput = TargetInput.Length > 1 ? TargetInput.Substring(0, TargetInput.Length - 1) : string.Empty;
            }
            if (!string.IsNullOrEmpty(SearchText))
            {
                SearchText = SearchText.Length > 1 ? SearchText.Substring(0, SearchText.Length - 1) : string.Empty;
            }

            await Task.Delay(stepDelay);
        }
    }

    [RelayCommand]
    public void ToggleFiltersPanel()
    {
        IsFiltersPanelOpen = !IsFiltersPanelOpen;
    }

    private void SafeRefreshFindingsView()
    {
        if (_findingsView == null) return;

        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.FromThread(Thread.CurrentThread);
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => _findingsView.Refresh());
        }
        else
        {
            _findingsView.Refresh();
        }
    }

    [RelayCommand]
    public void SetFilter(string filter)
    {
        ActiveFilter = filter;
        SafeRefreshFindingsView();
    }

    partial void OnSearchTextChanged(string value)
    {
        SafeRefreshFindingsView();
    }

    private bool FilterFindingItem(object item)
    {
        if (item is not FindingItemViewModel finding)
            return false;

        // 1. Category filter
        if (ActiveFilter != "All")
        {
            if (string.Equals(ActiveFilter, "DNS", StringComparison.OrdinalIgnoreCase) && !finding.IsDns)
                return false;
            if (string.Equals(ActiveFilter, "Whois", StringComparison.OrdinalIgnoreCase) && !finding.IsWhois)
                return false;
            if (string.Equals(ActiveFilter, "Subdomains", StringComparison.OrdinalIgnoreCase) && !finding.IsSubdomain)
                return false;
            if (string.Equals(ActiveFilter, "Tech", StringComparison.OrdinalIgnoreCase) && !finding.IsHttpTech)
                return false;
            if (string.Equals(ActiveFilter, "Other", StringComparison.OrdinalIgnoreCase) &&
                (finding.IsDns || finding.IsWhois || finding.IsSubdomain || finding.IsHttpTech))
                return false;
        }

        // 2. Search query filter
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var query = SearchText.Trim();
        if (finding.Type.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;
        if (finding.ProviderKey.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;
        if (finding.RawJson.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;

        // DNS checks
        if (finding.DnsData?.Value.Contains(query, StringComparison.OrdinalIgnoreCase) == true) return true;
        if (finding.DnsData?.Name.Contains(query, StringComparison.OrdinalIgnoreCase) == true) return true;

        // Whois checks
        if (finding.WhoisData?.Registrar.Contains(query, StringComparison.OrdinalIgnoreCase) == true) return true;
        if (finding.WhoisData?.DomainName.Contains(query, StringComparison.OrdinalIgnoreCase) == true) return true;

        // Subdomain checks
        if (finding.SubdomainData?.Subdomain.Contains(query, StringComparison.OrdinalIgnoreCase) == true) return true;
        if (finding.SubdomainData?.Issuer.Contains(query, StringComparison.OrdinalIgnoreCase) == true) return true;

        // HTTP Tech checks
        if (finding.HttpTechData?.Url.Contains(query, StringComparison.OrdinalIgnoreCase) == true) return true;
        if (finding.HttpTechData?.Title.Contains(query, StringComparison.OrdinalIgnoreCase) == true) return true;
        if (finding.HttpTechData?.Server.Contains(query, StringComparison.OrdinalIgnoreCase) == true) return true;
        if (finding.HttpTechData?.PoweredBy.Contains(query, StringComparison.OrdinalIgnoreCase) == true) return true;

        return false;
    }

    private void HandleStreamMessage(SearchStreamResponse response)
    {
        switch (response.PayloadCase)
        {
            case SearchStreamResponse.PayloadOneofCase.SessionStarted:
                var started = response.SessionStarted;
                SessionStatus = "Running";
                StatusMessage = $"Поиск запущен для цели: {started.TargetValue}";
                break;

            case SearchStreamResponse.PayloadOneofCase.FindingDiscovered:
                var discovered = response.FindingDiscovered;
                var model = new FindingModel
                {
                    FindingId = discovered.FindingId,
                    ProviderKey = discovered.ProviderKey,
                    Type = discovered.Type,
                    DataJson = discovered.DataJson,
                    CreatedAt = discovered.CreatedAt
                };
                var vm = new FindingItemViewModel(model);

                var generationAtDiscovered = _searchSessionGeneration;
                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    if (generationAtDiscovered != _searchSessionGeneration)
                    {
                        // Discard findings from canceled or previous search session
                        return;
                    }

                    lock (_findingsLock)
                    {
                        Findings.Add(vm);
                    }
                    TotalFindingsCount++;
                    if (vm.IsDns) DnsFindingsCount++;
                    else if (vm.IsWhois) WhoisFindingsCount++;
                    else if (vm.IsSubdomain) SubdomainFindingsCount++;
                    else if (vm.IsHttpTech) HttpTechFindingsCount++;
                });
                break;

            case SearchStreamResponse.PayloadOneofCase.SessionFinished:
                var finished = response.SessionFinished;
                SessionStatus = string.IsNullOrWhiteSpace(finished.Status) ? "Completed" : finished.Status;
                StatusMessage = $"Сессия завершена. Всего находок: {finished.TotalFindings}";
                IsRunning = false;
                _stopwatch.Stop();
                _timer.Stop();
                break;
        }
    }

    public void LoadExistingSession(SearchSessionModel session)
    {
        StopSearch();
        _searchSessionGeneration++;

        CurrentSessionId = session.EffectiveId;
        TargetInput = session.EffectiveTarget;
        ActiveTarget = session.EffectiveTarget;
        SessionStatus = session.Status;
        StatusMessage = $"Загружена сессия [{session.EffectiveId}] из истории. Статус: {session.Status}";

        lock (_findingsLock)
        {
            Findings.Clear();
            foreach (var f in session.Findings)
            {
                Findings.Add(new FindingItemViewModel(f));
            }
        }

        TotalFindingsCount = Findings.Count;
        DnsFindingsCount = Findings.Count(f => f.IsDns);
        WhoisFindingsCount = Findings.Count(f => f.IsWhois);
        SubdomainFindingsCount = Findings.Count(f => f.IsSubdomain);
        HttpTechFindingsCount = Findings.Count(f => f.IsHttpTech);

        SafeRefreshFindingsView();
    }

    private void CancelCurrentSearch()
    {
        try
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = null;
        }
        catch
        {
            // Ignore disposal errors
        }
    }
}
