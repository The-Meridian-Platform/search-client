using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Meridian.Client.Grpc;
using MeridianClient.Models;
using MeridianClient.Services;

namespace MeridianClient.ViewModels;

public partial class GraphViewModel : ObservableObject
{
    private readonly SearchViewModel _searchViewModel;
    private readonly HistoryViewModel _historyViewModel;
    private readonly IMeridianApiClient _apiClient;
    private readonly IMeridianStreamClient _streamClient;
    private readonly IInvestigationService _investigationService;
    private readonly ILogService _logService;
    private readonly GraphSimulationEngine _physics = new();
    private readonly Random _random = new();

    private readonly Dictionary<string, GraphNode> _nodeMap = new();
    private readonly DispatcherTimer _physicsTimer;
    private CancellationTokenSource? _pivotCts;

    public ObservableCollection<GraphNode> Nodes { get; } = new();
    public ObservableCollection<GraphEdge> Edges { get; } = new();
    public ObservableCollection<InvestigationModel> AvailableInvestigations { get; } = new();
    public ObservableCollection<string> CurrentInvestigationTargets { get; } = new();

    [ObservableProperty]
    private GraphNode? _selectedNode;

    [ObservableProperty]
    private bool _hasSelectedNode;

    [ObservableProperty]
    private bool _isLiveMode = true;

    [ObservableProperty]
    private bool _isInvestigationMode;

    [ObservableProperty]
    private InvestigationModel? _currentInvestigation;

    [ObservableProperty]
    private string _currentSessionTitle = "Текущий поиск (Live)";

    [ObservableProperty]
    private string _targetDisplay = "—";

    [ObservableProperty]
    private bool _isPhysicsRunning = true;

    [ObservableProperty]
    private string _nodeSearchText = string.Empty;

    [ObservableProperty]
    private int _nodesCount;

    [ObservableProperty]
    private int _edgesCount;

    [ObservableProperty]
    private bool _isLoadingSession;

    [ObservableProperty]
    private bool _isPivoting;

    [ObservableProperty]
    private string _pivotingTarget = string.Empty;

    [ObservableProperty]
    private int _pivotFindingsCount;

    public ObservableCollection<SearchSessionModel> AvailableSessions => _historyViewModel.Sessions;

    private SearchSessionModel? _selectedSession;
    public SearchSessionModel? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (SetProperty(ref _selectedSession, value) && value != null)
            {
                _ = LoadSessionByIdAsync(value.EffectiveId, value.EffectiveTarget);
            }
        }
    }

    private InvestigationModel? _selectedInvestigation;
    public InvestigationModel? SelectedInvestigation
    {
        get => _selectedInvestigation;
        set
        {
            if (SetProperty(ref _selectedInvestigation, value) && value != null)
            {
                _ = LoadInvestigationAsync(value);
            }
        }
    }

    public event Action? RequestCenterView;

    public GraphViewModel(
        SearchViewModel searchViewModel,
        HistoryViewModel historyViewModel,
        IMeridianApiClient apiClient,
        IMeridianStreamClient streamClient,
        IInvestigationService investigationService,
        ILogService logService)
    {
        _searchViewModel = searchViewModel;
        _historyViewModel = historyViewModel;
        _apiClient = apiClient;
        _streamClient = streamClient;
        _investigationService = investigationService;
        _logService = logService;

        _investigationService.InvestigationsChanged += ReloadAvailableInvestigations;
        ReloadAvailableInvestigations();

        // Physics step timer (smooth 60 FPS)
        _physicsTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _physicsTimer.Tick += OnPhysicsTick;
        _physicsTimer.Start();

        // Subscribe to live search findings
        ((INotifyCollectionChanged)_searchViewModel.Findings).CollectionChanged += OnLiveFindingsChanged;

        // Initialize with initial live data
        SyncFromLiveSearch();
    }

    private void OnLiveFindingsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!IsLiveMode) return;

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            ClearGraph();
            string target = !string.IsNullOrWhiteSpace(_searchViewModel.ActiveTarget)
                ? _searchViewModel.ActiveTarget
                : _searchViewModel.TargetInput;
            TargetDisplay = !string.IsNullOrWhiteSpace(target) ? target : "—";
            return;
        }

        if (e.NewItems != null)
        {
            string target = !string.IsNullOrWhiteSpace(_searchViewModel.ActiveTarget)
                ? _searchViewModel.ActiveTarget
                : _searchViewModel.TargetInput;

            if (string.IsNullOrWhiteSpace(target) && e.NewItems.Count > 0 && e.NewItems[0] is FindingItemViewModel f)
            {
                target = GraphDataMapper.DetectTargetDomain(f);
            }

            TargetDisplay = !string.IsNullOrWhiteSpace(target) ? target : "—";

            foreach (FindingItemViewModel item in e.NewItems)
            {
                AddFindingToGraph(item, TargetDisplay);
            }
            _physics.WakeUp();
            UpdateCounts();
        }
    }

    private void OnPhysicsTick(object? sender, EventArgs e)
    {
        if (!IsPhysicsRunning) return;

        // Run one physics step
        double energy = _physics.Step(Nodes, Edges, 400, 300);
    }

    public void SyncFromLiveSearch()
    {
        IsLiveMode = true;
        CurrentSessionTitle = "Текущий поиск (Live)";

        string target = !string.IsNullOrWhiteSpace(_searchViewModel.ActiveTarget)
            ? _searchViewModel.ActiveTarget
            : (!string.IsNullOrWhiteSpace(_searchViewModel.TargetInput) ? _searchViewModel.TargetInput : string.Empty);

        if (string.IsNullOrWhiteSpace(target))
        {
            var first = _searchViewModel.Findings.FirstOrDefault();
            if (first != null)
            {
                target = GraphDataMapper.DetectTargetDomain(first);
            }
        }

        TargetDisplay = !string.IsNullOrWhiteSpace(target) ? target : "—";

        ClearGraph();

        if (_searchViewModel.Findings.Count == 0)
        {
            UpdateCounts();
            return;
        }

        lock (Nodes)
        {
            var snapshot = _searchViewModel.Findings.ToList();
            foreach (var finding in snapshot)
            {
                AddFindingToGraph(finding, TargetDisplay);
            }
        }

        _physics.WakeUp();
        UpdateCounts();
        RequestCenterView?.Invoke();
    }

    public async Task LoadSessionByIdAsync(string sessionId, string target)
    {
        IsLoadingSession = true;
        IsLiveMode = false;

        try
        {
            var detail = await _apiClient.GetSessionDetailsAsync(sessionId);
            if (detail != null)
            {
                ClearGraph();

                string resolvedTarget = !string.IsNullOrWhiteSpace(detail.EffectiveTarget)
                    ? detail.EffectiveTarget
                    : (!string.IsNullOrWhiteSpace(target) ? target : string.Empty);

                if (string.IsNullOrWhiteSpace(resolvedTarget) && detail.Findings.Count > 0)
                {
                    resolvedTarget = GraphDataMapper.DetectTargetDomain(new FindingItemViewModel(detail.Findings[0]));
                }

                TargetDisplay = !string.IsNullOrWhiteSpace(resolvedTarget) ? resolvedTarget : "—";
                CurrentSessionTitle = $"Сессия {sessionId[..Math.Min(8, sessionId.Length)]}... ({TargetDisplay})";

                foreach (var finding in detail.Findings)
                {
                    AddFindingToGraph(new FindingItemViewModel(finding), TargetDisplay);
                }
                _physics.WakeUp();
                UpdateCounts();
                RequestCenterView?.Invoke();
                _logService.LogInfo("Graph", $"Загружен граф для сессии [{sessionId}], узлов: {Nodes.Count}, связей: {Edges.Count}");
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("Graph", $"Не удалось загрузить граф для сессии {sessionId}: {ex.Message}", ex);
        }
        finally
        {
            IsLoadingSession = false;
        }
    }

    private void AddFindingToGraph(FindingItemViewModel finding, string target)
    {
        var newEdges = new List<GraphEdge>();
        GraphDataMapper.MapFindingToGraph(finding, target, _nodeMap, newEdges, _random, 400, 300);

        foreach (var node in _nodeMap.Values)
        {
            if (!Nodes.Contains(node))
            {
                Nodes.Add(node);
            }
        }

        foreach (var edge in newEdges)
        {
            if (!Edges.Contains(edge))
            {
                Edges.Add(edge);
            }
        }
    }

    private void ClearGraph()
    {
        _nodeMap.Clear();
        Nodes.Clear();
        Edges.Clear();
        SelectedNode = null;
        HasSelectedNode = false;
        UpdateCounts();
    }

    private void UpdateCounts()
    {
        NodesCount = Nodes.Count;
        EdgesCount = Edges.Count;
    }

    partial void OnSelectedNodeChanged(GraphNode? value)
    {
        HasSelectedNode = value != null;

        foreach (var node in Nodes)
        {
            node.IsSelected = (node == value);
            node.IsHighlighted = false;
        }

        foreach (var edge in Edges)
        {
            edge.IsHighlighted = false;
        }

        if (value != null)
        {
            // Highlight connected edges and neighboring nodes
            foreach (var edge in Edges)
            {
                if (edge.Source == value || edge.Target == value)
                {
                    edge.IsHighlighted = true;
                    edge.Source.IsHighlighted = true;
                    edge.Target.IsHighlighted = true;
                }
            }
        }
    }

    partial void OnNodeSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            foreach (var node in Nodes) node.IsHighlighted = false;
            return;
        }

        string q = value.Trim();
        foreach (var node in Nodes)
        {
            node.IsHighlighted = node.Label.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                 node.SubLabel.Contains(q, StringComparison.OrdinalIgnoreCase);
        }
    }

    [RelayCommand]
    public void SelectNode(GraphNode? node)
    {
        SelectedNode = node;
    }

    [RelayCommand]
    public void ClearSelection()
    {
        SelectedNode = null;
    }

    [RelayCommand]
    public void TogglePhysics()
    {
        IsPhysicsRunning = !IsPhysicsRunning;
        if (IsPhysicsRunning)
        {
            _physics.WakeUp();
        }
    }

    [RelayCommand]
    public void ResetLayout()
    {
        // Place nodes radially around center
        double cx = 400, cy = 300;
        int count = Nodes.Count;
        for (int i = 0; i < count; i++)
        {
            var node = Nodes[i];
            if (node.Type == GraphNodeType.Target)
            {
                node.X = cx;
                node.Y = cy;
            }
            else
            {
                double angle = (2 * Math.PI * i) / Math.Max(count - 1, 1);
                double radius = 120 + (i % 3) * 60;
                node.X = cx + Math.Cos(angle) * radius;
                node.Y = cy + Math.Sin(angle) * radius;
                node.Vx = 0;
                node.Vy = 0;
            }
        }
        _physics.WakeUp();
    }

    [RelayCommand]
    public void SwitchToLiveMode()
    {
        SelectedSession = null;
        SyncFromLiveSearch();
    }

    [RelayCommand]
    public void CopyNodeValue()
    {
        if (SelectedNode != null)
        {
            try
            {
                Clipboard.SetText(SelectedNode.Label);
            }
            catch { }
        }
    }

    [RelayCommand]
    public void CopyNodeJson()
    {
        if (SelectedNode?.RawJson != null)
        {
            try
            {
                Clipboard.SetText(SelectedNode.RawJson);
            }
            catch { }
        }
    }

    [RelayCommand]
    public void StartSearchOnNode()
    {
        if (SelectedNode != null)
        {
            _searchViewModel.TargetInput = SelectedNode.Label;
            // Switch to search tab
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
                {
                    mainVm.NavigateTo("Search");
                }
            });
        }
    }

    [RelayCommand]
    public async Task PivotSearchOnNodeAsync()
    {
        if (SelectedNode == null || IsPivoting) return;

        var pivotNode = SelectedNode;
        string pivotTarget = pivotNode.Label.Trim();
        if (string.IsNullOrWhiteSpace(pivotTarget)) return;

        CancelPivot();
        _pivotCts = new CancellationTokenSource();
        var ct = _pivotCts.Token;

        IsPivoting = true;
        PivotingTarget = pivotTarget;
        PivotFindingsCount = 0;

        _logService.LogInfo("Pivot", $"Запуск Pivot-исследования от узла [{pivotTarget}] (тип: {pivotNode.TypeNameRu})");

        try
        {
            EnsureCurrentInvestigationExists();

            // 1. Create session via REST API
            var sessionResponse = await _apiClient.CreateSessionAsync(pivotTarget, "Auto", ct);
            string sessionId = sessionResponse.EffectiveId;

            if (CurrentInvestigation != null && !CurrentInvestigation.SessionIds.Contains(sessionId))
            {
                CurrentInvestigation.SessionIds.Add(sessionId);
            }
            if (!CurrentInvestigationTargets.Contains(pivotTarget))
            {
                CurrentInvestigationTargets.Add(pivotTarget);
            }

            // 2. Open gRPC stream and listen for findings
            var stream = _streamClient.SubscribeToSessionAsync(sessionId, ct);

            await foreach (var message in stream.WithCancellation(ct))
            {
                if (message.PayloadCase == SearchStreamResponse.PayloadOneofCase.FindingDiscovered)
                {
                    var discovered = message.FindingDiscovered;
                    var model = new FindingModel
                    {
                        FindingId = discovered.FindingId,
                        ProviderKey = discovered.ProviderKey,
                        Type = discovered.Type,
                        DataJson = discovered.DataJson,
                        CreatedAt = discovered.CreatedAt
                    };

                    if (CurrentInvestigation != null && !CurrentInvestigation.CachedFindings.Any(f => f.EffectiveId == model.EffectiveId))
                    {
                        CurrentInvestigation.CachedFindings.Add(model);
                    }

                    var findingVm = new FindingItemViewModel(model);

                    Application.Current?.Dispatcher.InvokeAsync(() =>
                    {
                        if (ct.IsCancellationRequested) return;

                        var newEdges = new List<GraphEdge>();
                        GraphDataMapper.MapFindingToGraph(findingVm, pivotTarget, _nodeMap, newEdges, _random, pivotNode.X, pivotNode.Y);

                        foreach (var node in _nodeMap.Values)
                        {
                            if (!Nodes.Contains(node))
                            {
                                Nodes.Add(node);
                            }
                        }

                        // Also connect pivotNode with the new target node if distinct
                        string targetNodeId = $"target:{pivotTarget.ToLowerInvariant().Trim()}";
                        if (_nodeMap.TryGetValue(targetNodeId, out var newTargetNode) && newTargetNode != pivotNode)
                        {
                            string bridgeEdgeId = $"{pivotNode.Id}->{newTargetNode.Id}:Pivot";
                            if (!Edges.Any(e => e.Id == bridgeEdgeId))
                            {
                                var bridgeEdge = new GraphEdge(pivotNode, newTargetNode, "Pivot связь");
                                Edges.Add(bridgeEdge);
                            }
                        }

                        foreach (var edge in newEdges)
                        {
                            if (!Edges.Contains(edge))
                            {
                                Edges.Add(edge);
                            }
                        }

                        PivotFindingsCount++;
                        UpdateCounts();
                        _physics.WakeUp();
                    });
                }
            }

            if (CurrentInvestigation != null)
            {
                _investigationService.Save(CurrentInvestigation);
                ReloadAvailableInvestigations();
            }

            _logService.LogInfo("Pivot", $"Pivot-исследование узла [{pivotTarget}] завершено. Находок: {PivotFindingsCount}");
        }
        catch (OperationCanceledException)
        {
            _logService.LogWarning("Pivot", $"Pivot-исследование [{pivotTarget}] отменено пользователем.");
        }
        catch (Exception ex)
        {
            _logService.LogError("Pivot", $"Ошибка Pivot-исследования узла [{pivotTarget}]: {ex.Message}", ex);
        }
        finally
        {
            IsPivoting = false;
            PivotingTarget = string.Empty;
        }
    }

    [RelayCommand]
    public void CancelPivot()
    {
        try
        {
            _pivotCts?.Cancel();
            _pivotCts?.Dispose();
        }
        catch { }
        _pivotCts = null;
        IsPivoting = false;
    }

    [RelayCommand]
    public void ClearCanvasGraph()
    {
        CancelPivot();
        ClearGraph();
        TargetDisplay = "—";
        SelectedSession = null;
        SelectedInvestigation = null;
        CurrentInvestigation = null;
        CurrentInvestigationTargets.Clear();
        IsInvestigationMode = false;
    }

    public void ReloadAvailableInvestigations()
    {
        var list = _investigationService.GetAll();
        AvailableInvestigations.Clear();
        foreach (var item in list)
        {
            AvailableInvestigations.Add(item);
        }
    }

    public void EnsureCurrentInvestigationExists()
    {
        if (CurrentInvestigation == null)
        {
            string target = !string.IsNullOrWhiteSpace(TargetDisplay) && TargetDisplay != "—"
                ? TargetDisplay
                : "Новое расследование";

            CurrentInvestigation = new InvestigationModel
            {
                Title = $"Расследование: {target}",
                RootTarget = target,
                SessionIds = new List<string>()
            };

            if (!string.IsNullOrWhiteSpace(_searchViewModel.CurrentSessionId))
            {
                CurrentInvestigation.SessionIds.Add(_searchViewModel.CurrentSessionId);
            }

            if (!string.IsNullOrWhiteSpace(target) && target != "—")
            {
                CurrentInvestigationTargets.Add(target);
            }

            _investigationService.Save(CurrentInvestigation);
            ReloadAvailableInvestigations();
        }
    }

    public async Task LoadInvestigationAsync(InvestigationModel inv)
    {
        IsLoadingSession = true;
        IsLiveMode = false;
        IsInvestigationMode = true;
        CurrentInvestigation = inv;
        CurrentSessionTitle = inv.Title;
        TargetDisplay = inv.RootTarget;

        ClearGraph();
        CurrentInvestigationTargets.Clear();
        if (!string.IsNullOrWhiteSpace(inv.RootTarget))
        {
            CurrentInvestigationTargets.Add(inv.RootTarget);
        }

        try
        {
            var findingsToLoad = new List<FindingModel>(inv.CachedFindings);

            // Attempt to fetch fresh findings from API for all SessionIds
            if (inv.SessionIds.Count > 0)
            {
                foreach (var sid in inv.SessionIds)
                {
                    try
                    {
                        var detail = await _apiClient.GetSessionDetailsAsync(sid);
                        if (detail != null)
                        {
                            if (!string.IsNullOrWhiteSpace(detail.EffectiveTarget) && !CurrentInvestigationTargets.Contains(detail.EffectiveTarget))
                            {
                                CurrentInvestigationTargets.Add(detail.EffectiveTarget);
                            }

                            foreach (var f in detail.Findings)
                            {
                                if (!findingsToLoad.Any(existing => existing.EffectiveId == f.EffectiveId))
                                {
                                    findingsToLoad.Add(f);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            // Map all findings to canvas
            foreach (var finding in findingsToLoad)
            {
                AddFindingToGraph(new FindingItemViewModel(finding), inv.RootTarget);
            }

            inv.CachedFindings = findingsToLoad;
            _investigationService.Save(inv);

            _physics.WakeUp();
            UpdateCounts();
            RequestCenterView?.Invoke();
            _logService.LogInfo("Graph", $"Загружено расследование [{inv.Title}]: {Nodes.Count} узлов, {Edges.Count} связей");
        }
        catch (Exception ex)
        {
            _logService.LogError("Graph", $"Не удалось загрузить расследование {inv.Id}: {ex.Message}", ex);
        }
        finally
        {
            IsLoadingSession = false;
        }
    }

    [RelayCommand]
    public void ExportInvestigation()
    {
        EnsureCurrentInvestigationExists();
        if (CurrentInvestigation == null) return;

        var sfd = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Экспорт расследования Meridian",
            Filter = "Файл расследования Meridian (*.json)|*.json",
            FileName = $"{SanitizeFileName(CurrentInvestigation.Title)}.json"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                _investigationService.ExportToFile(CurrentInvestigation, sfd.FileName);
                _logService.LogInfo("Graph", $"Расследование экспортировано в: {sfd.FileName}");
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Views.ConfirmDialogWindow.Show(
                        "Экспорт расследования",
                        "Расследование успешно сохранено в файл!",
                        $"Путь: {sfd.FileName}\n\nФайлом можно поделиться с коллегой — MeridianClient развернет всю топологию графа и сохраненные находки.",
                        confirmText: "Отлично",
                        cancelText: "Закрыть",
                        isDanger: false,
                        icon: Wpf.Ui.Controls.SymbolRegular.Save24);
                });
            }
            catch (Exception ex)
            {
                _logService.LogError("Graph", $"Ошибка экспорта: {ex.Message}", ex);
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Views.ConfirmDialogWindow.Show(
                        "Ошибка экспорта",
                        "Не удалось сохранить файл расследования",
                        ex.Message,
                        confirmText: "Понятно",
                        cancelText: "Закрыть",
                        isDanger: true,
                        icon: Wpf.Ui.Controls.SymbolRegular.Warning24);
                });
            }
        }
    }

    [RelayCommand]
    public void ImportInvestigation()
    {
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Импорт расследования Meridian",
            Filter = "Файл расследования Meridian (*.json)|*.json"
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                var imported = _investigationService.ImportFromFile(ofd.FileName);
                ReloadAvailableInvestigations();
                SelectedInvestigation = AvailableInvestigations.FirstOrDefault(i => i.Id == imported.Id);
                _logService.LogInfo("Graph", $"Расследование импортировано: {imported.Title}");
            }
            catch (Exception ex)
            {
                _logService.LogError("Graph", $"Ошибка импорта: {ex.Message}", ex);
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Views.ConfirmDialogWindow.Show(
                        "Ошибка импорта",
                        "Не удалось открыть файл расследования",
                        ex.Message,
                        confirmText: "Понятно",
                        cancelText: "Закрыть",
                        isDanger: true,
                        icon: Wpf.Ui.Controls.SymbolRegular.Warning24);
                });
            }
        }
    }

    [RelayCommand]
    public void DeleteInvestigation(InvestigationModel? inv)
    {
        var target = inv ?? SelectedInvestigation ?? CurrentInvestigation;
        if (target == null) return;

        bool confirmed = false;
        Application.Current?.Dispatcher.Invoke(() =>
        {
            confirmed = Views.ConfirmDialogWindow.Show(
                "Удаление расследования",
                $"Вы действительно хотите удалить проект «{target.Title}»?",
                "Все сохраненные ветки, связи и кэш находок этого расследования будут безвозвратно удалены.",
                confirmText: "Удалить расследование",
                cancelText: "Отмена",
                isDanger: true,
                icon: Wpf.Ui.Controls.SymbolRegular.Delete24);
        });

        if (confirmed)
        {
            _investigationService.Delete(target.Id);
            ReloadAvailableInvestigations();

            if (SelectedInvestigation?.Id == target.Id || CurrentInvestigation?.Id == target.Id)
            {
                ClearCanvasGraph();
            }

            _logService.LogInfo("Investigation", $"Удалено расследование [{target.Title}]");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalids = System.IO.Path.GetInvalidFileNameChars();
        var clean = string.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).Trim();
        return string.IsNullOrWhiteSpace(clean) ? "investigation" : clean;
    }
}
