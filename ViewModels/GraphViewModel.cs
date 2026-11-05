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
    private readonly IToastService _toastService;
    private readonly ISettingsService _settingsService;
    private readonly GraphSimulationEngine _physics = new();
    private readonly Random _random = new();

    private readonly Dictionary<string, GraphNode> _nodeMap = new();
    private readonly HashSet<string> _visitedTargets = new(StringComparer.OrdinalIgnoreCase);
    private const int MaxCanvasNodes = 150;

    private static readonly HashSet<string> SupernodeBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "google.com", "gmail.com", "googlemail.com",
        "yandex.ru", "yandex.com", "yandex.net", "ya.ru",
        "outlook.com", "hotmail.com", "live.com", "msn.com", "microsoft.com",
        "protonmail.com", "proton.me",
        "mail.ru", "inbox.ru", "bk.ru", "list.ru",
        "yahoo.com", "icloud.com", "me.com", "mac.com",
        "rambler.ru", "zoho.com", "gmx.com", "gmx.net", "aol.com",
        "tutanota.com", "tuta.io", "fastmail.com",
        "cloudflare.com", "one.one.one.one", "dns.google", "quad9.net", "opendns.com",
        "loopia.se", "godaddy.com", "namecheap.com", "reg.ru", "nic.ru",
        "awsdns", "digitalocean.com"
    };

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

    [ObservableProperty]
    private bool _isAutoExpandEnabled;

    public string AutoExpandButtonText => IsAutoExpandEnabled
        ? LocalizationService.Instance.GetString("Graph_AutoExpand_On")
        : LocalizationService.Instance.GetString("Graph_AutoExpand_Off");

    partial void OnIsAutoExpandEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(AutoExpandButtonText));
    }

    [RelayCommand]
    public void ToggleAutoExpand()
    {
        IsAutoExpandEnabled = !IsAutoExpandEnabled;
    }

    [ObservableProperty]
    private bool _hasErrorMessage;

    [ObservableProperty]
    private bool _isToastSuccess;

    public string SmartLayoutToolTip => LocalizationService.Instance.GetString("Graph_Btn_SmartLayout_ToolTip");

    public ObservableCollection<SearchSessionModel> AvailableSessions => _historyViewModel.Sessions;

    private string? _lastSelectedSessionId;
    private SearchSessionModel? _selectedSession;
    public SearchSessionModel? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (value != null)
            {
                _lastSelectedSessionId = value.EffectiveId;
            }
            if (SetProperty(ref _selectedSession, value) && value != null)
            {
                _selectedInvestigation = null;
                OnPropertyChanged(nameof(SelectedInvestigation));
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
                _selectedSession = null;
                OnPropertyChanged(nameof(SelectedSession));
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
        ILogService logService,
        ISettingsService settingsService,
        IToastService? toastService = null)
    {
        _searchViewModel = searchViewModel;
        _historyViewModel = historyViewModel;
        _apiClient = apiClient;
        _streamClient = streamClient;
        _investigationService = investigationService;
        _logService = logService;
        _settingsService = settingsService;
        _toastService = toastService ?? new ToastService(settingsService);

        Nodes.CollectionChanged += (s, e) => NodesCount = Nodes.Count;
        Edges.CollectionChanged += (s, e) => EdgesCount = Edges.Count;

        _investigationService.InvestigationsChanged += ReloadAvailableInvestigations;
        ReloadAvailableInvestigations();

        _historyViewModel.Sessions.CollectionChanged += (_, _) =>
        {
            if (_selectedSession == null && !string.IsNullOrEmpty(_lastSelectedSessionId) && !IsLiveMode && !IsInvestigationMode)
            {
                var restored = _historyViewModel.Sessions.FirstOrDefault(s => s.EffectiveId == _lastSelectedSessionId);
                if (restored != null)
                {
                    _selectedSession = restored;
                    OnPropertyChanged(nameof(SelectedSession));
                }
            }
        };

        // Physics step timer (smooth 60 FPS)
        _physicsTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _physicsTimer.Tick += OnPhysicsTick;
        _physicsTimer.Start();

        // Subscribe to live search findings
        ((INotifyCollectionChanged)_searchViewModel.Findings).CollectionChanged += OnLiveFindingsChanged;
        _searchViewModel.PropertyChanged += OnSearchViewModelPropertyChanged;
        LocalizationService.Instance.LanguageChanged += () =>
        {
            OnPropertyChanged(nameof(AutoExpandButtonText));
            OnPropertyChanged(nameof(SmartLayoutToolTip));
        };

        // Initialize with initial live data
        SyncFromLiveSearch();
    }

    private void OnSearchViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchViewModel.IsRunning) && !_searchViewModel.IsRunning && _searchViewModel.SessionStatus == "Completed")
        {
            if (IsAutoExpandEnabled && IsLiveMode)
            {
                _ = Application.Current?.Dispatcher.InvokeAsync(RunSmartAutoExpandAsync);
            }
        }
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

            if (snapshot.Count > 0)
            {
                EnsureCurrentInvestigationExists();
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
        IsInvestigationMode = false;
        CurrentInvestigation = null;

        try
        {
            var detail = await _apiClient.GetSessionDetailsAsync(sessionId);
            if (detail == null && AvailableSessions.FirstOrDefault(s => s.EffectiveId == sessionId) is { } cachedSession)
            {
                detail = cachedSession;
            }

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
        if (!string.IsNullOrWhiteSpace(target) && target != "—")
        {
            _visitedTargets.Add(SanitizeTarget(target));
        }

        var newEdges = new List<GraphEdge>();
        bool mergeShared = _settingsService.Settings.MergeSharedGraphEntities;
        GraphDataMapper.MapFindingToGraph(finding, target, _nodeMap, newEdges, _random, 400, 300, mergeShared);

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
        _visitedTargets.Clear();
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
            edge.IsThreatPath = false;
        }

        if (value != null)
        {
            if (value.IsThreat)
            {
                HighlightThreatPathToRoots(value);
            }
            else
            {
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
    }

    private void HighlightThreatPathToRoots(GraphNode threatNode)
    {
        var parentEdges = new Dictionary<GraphNode, List<GraphEdge>>();
        foreach (var edge in Edges)
        {
            if (!parentEdges.ContainsKey(edge.Target)) parentEdges[edge.Target] = new List<GraphEdge>();
            parentEdges[edge.Target].Add(edge);
        }

        var queue = new Queue<GraphNode>();
        var visited = new HashSet<GraphNode>();
        queue.Enqueue(threatNode);
        visited.Add(threatNode);
        threatNode.IsHighlighted = true;

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            if (parentEdges.TryGetValue(curr, out var incoming))
            {
                foreach (var edge in incoming)
                {
                    edge.IsThreatPath = true;
                    edge.IsHighlighted = true;
                    edge.Source.IsHighlighted = true;
                    if (visited.Add(edge.Source))
                    {
                        queue.Enqueue(edge.Source);
                    }
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
    public void OrganizeAndFreezeLayout()
    {
        if (Nodes.Count == 0) return;

        IsPhysicsRunning = false;

        foreach (var node in Nodes)
        {
            node.Vx = 0;
            node.Vy = 0;
        }

        var inEdges = new Dictionary<GraphNode, List<GraphNode>>();
        var outEdges = new Dictionary<GraphNode, List<GraphNode>>();
        var allAdj = new Dictionary<GraphNode, List<GraphNode>>();

        foreach (var node in Nodes)
        {
            inEdges[node] = new List<GraphNode>();
            outEdges[node] = new List<GraphNode>();
            allAdj[node] = new List<GraphNode>();
        }

        foreach (var edge in Edges)
        {
            if (allAdj.ContainsKey(edge.Source) && allAdj.ContainsKey(edge.Target))
            {
                outEdges[edge.Source].Add(edge.Target);
                inEdges[edge.Target].Add(edge.Source);
                allAdj[edge.Source].Add(edge.Target);
                allAdj[edge.Target].Add(edge.Source);
            }
        }

        var roots = Nodes
            .Where(n => n.Type == GraphNodeType.Target && inEdges[n].Count == 0)
            .OrderBy(n => n.ExpansionDepth)
            .ThenByDescending(n => n.Label.Equals(TargetDisplay, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (roots.Count == 0)
        {
            roots = Nodes
                .Where(n => n.Type == GraphNodeType.Target)
                .OrderBy(n => n.ExpansionDepth)
                .ThenByDescending(n => n.Label.Equals(TargetDisplay, StringComparison.OrdinalIgnoreCase))
                .Take(1)
                .ToList();
        }

        if (roots.Count == 0)
        {
            roots = Nodes.Where(n => inEdges[n].Count == 0).ToList();
        }

        if (roots.Count == 0)
        {
            roots.Add(Nodes[0]);
        }

        var visited = new HashSet<GraphNode>();
        var treeChildren = new Dictionary<GraphNode, List<GraphNode>>();
        var nodeLevels = new Dictionary<GraphNode, int>();

        foreach (var node in Nodes)
        {
            treeChildren[node] = new List<GraphNode>();
        }

        var bfsQueue = new Queue<GraphNode>();
        foreach (var root in roots)
        {
            if (visited.Add(root))
            {
                nodeLevels[root] = 0;
                bfsQueue.Enqueue(root);
            }
        }

        while (bfsQueue.Count > 0)
        {
            var curr = bfsQueue.Dequeue();
            int currLevel = nodeLevels[curr];

            var candidates = outEdges[curr]
                .OrderBy(n => GetNodeTypePriority(n.Type))
                .ThenBy(n => n.Label)
                .ToList();

            foreach (var child in candidates)
            {
                if (visited.Add(child))
                {
                    nodeLevels[child] = currLevel + 1;
                    treeChildren[curr].Add(child);
                    bfsQueue.Enqueue(child);
                }
            }

            foreach (var parentLike in inEdges[curr].OrderBy(n => GetNodeTypePriority(n.Type)).ThenBy(n => n.Label))
            {
                if (visited.Add(parentLike))
                {
                    nodeLevels[parentLike] = currLevel + 1;
                    treeChildren[curr].Add(parentLike);
                    bfsQueue.Enqueue(parentLike);
                }
            }
        }

        foreach (var node in Nodes)
        {
            if (visited.Add(node))
            {
                nodeLevels[node] = 1;
                treeChildren[roots[0]].Add(node);
            }
        }

        var subtreeWidths = new Dictionary<GraphNode, double>();
        const double nodeSpacing = 160.0;
        const double levelHeight = 220.0;
        const double treeSpacing = 260.0;
        const double gridRowHeight = 90.0;

        double ComputeWidth(GraphNode node)
        {
            if (node.IsCollapsed)
            {
                subtreeWidths[node] = nodeSpacing;
                return nodeSpacing;
            }

            var children = treeChildren[node].Where(c => c.IsVisible).ToList();
            if (children.Count == 0)
            {
                subtreeWidths[node] = nodeSpacing;
                return nodeSpacing;
            }

            var typeGroups = children.GroupBy(c => c.Type).ToList();
            double sumW = 0.0;

            foreach (var group in typeGroups)
            {
                var groupItems = group.ToList();
                bool isLeafGroup = groupItems.All(item => !treeChildren[item].Any(c => c.IsVisible));

                if (isLeafGroup && groupItems.Count >= 3)
                {
                    int columns = Math.Min(4, (int)Math.Ceiling(Math.Sqrt(groupItems.Count)));
                    sumW += columns * nodeSpacing;
                }
                else
                {
                    foreach (var ch in groupItems)
                    {
                        sumW += ComputeWidth(ch);
                    }
                }
            }

            double w = Math.Max(nodeSpacing, sumW);
            subtreeWidths[node] = w;
            return w;
        }

        foreach (var root in roots)
        {
            ComputeWidth(root);
        }

        double totalWidth = 0.0;
        for (int i = 0; i < roots.Count; i++)
        {
            totalWidth += subtreeWidths[roots[i]];
            if (i < roots.Count - 1) totalWidth += treeSpacing;
        }

        double currentStartX = 2500.0 - totalWidth / 2.0;
        double startY = 2200.0;

        void PositionSubtree(GraphNode node, double leftX, double y)
        {
            node.Y = y;
            node.Vx = 0;
            node.Vy = 0;

            if (node.IsCollapsed)
            {
                node.X = leftX + subtreeWidths[node] / 2.0;
                return;
            }

            var children = treeChildren[node].Where(c => c.IsVisible).ToList();
            if (children.Count == 0)
            {
                node.X = leftX + subtreeWidths[node] / 2.0;
                return;
            }

            var typeGroups = children.GroupBy(c => c.Type).ToList();
            double blockX = leftX;
            double firstChildCenterX = double.NaN;
            double lastChildCenterX = double.NaN;

            foreach (var group in typeGroups)
            {
                var groupItems = group.ToList();
                bool isLeafGroup = groupItems.All(item => !treeChildren[item].Any(c => c.IsVisible));

                if (isLeafGroup && groupItems.Count >= 3)
                {
                    int columns = Math.Min(4, (int)Math.Ceiling(Math.Sqrt(groupItems.Count)));
                    double groupWidth = columns * nodeSpacing;

                    for (int idx = 0; idx < groupItems.Count; idx++)
                    {
                        var item = groupItems[idx];
                        int col = idx % columns;
                        int row = idx / columns;

                        item.X = blockX + (col + 0.5) * nodeSpacing;
                        item.Y = y + levelHeight + row * gridRowHeight;
                        item.Vx = 0;
                        item.Vy = 0;

                        if (double.IsNaN(firstChildCenterX)) firstChildCenterX = item.X;
                        lastChildCenterX = item.X;
                    }

                    blockX += groupWidth;
                }
                else
                {
                    for (int i = 0; i < groupItems.Count; i++)
                    {
                        var ch = groupItems[i];
                        double chW = subtreeWidths[ch];
                        PositionSubtree(ch, blockX, y + levelHeight);

                        if (double.IsNaN(firstChildCenterX)) firstChildCenterX = ch.X;
                        lastChildCenterX = ch.X;

                        blockX += chW;
                    }
                }
            }

            if (!double.IsNaN(firstChildCenterX) && !double.IsNaN(lastChildCenterX))
            {
                node.X = (firstChildCenterX + lastChildCenterX) / 2.0;
            }
            else
            {
                node.X = leftX + subtreeWidths[node] / 2.0;
            }
        }

        foreach (var root in roots)
        {
            double rW = subtreeWidths[root];
            PositionSubtree(root, currentStartX, startY);
            currentStartX += rW + treeSpacing;
        }

        const double safeWidth = 140.0;
        for (int iter = 0; iter < 30; iter++)
        {
            bool anyOverlap = false;
            for (int i = 0; i < Nodes.Count; i++)
            {
                var a = Nodes[i];
                for (int j = i + 1; j < Nodes.Count; j++)
                {
                    var b = Nodes[j];
                    if (Math.Abs(a.Y - b.Y) > 80.0) continue;

                    double dx = b.X - a.X;
                    double overlapX = safeWidth - Math.Abs(dx);

                    if (overlapX > 0)
                    {
                        anyOverlap = true;
                        double signX = dx >= 0 ? 1.0 : -1.0;
                        if (Math.Abs(dx) < 0.001) signX = (i % 2 == 0) ? 1.0 : -1.0;

                        double shiftX = (overlapX * 0.5) * signX;
                        if (!a.IsPinned && !roots.Contains(a))
                        {
                            a.X -= shiftX * 0.5;
                        }
                        if (!b.IsPinned && !roots.Contains(b))
                        {
                            b.X += shiftX * 0.5;
                        }
                    }
                }
            }
            if (!anyOverlap) break;
        }

        foreach (var node in Nodes)
        {
            node.Vx = 0;
            node.Vy = 0;
        }

        RequestCenterView?.Invoke();

        ShowToast(
            LocalizationService.Instance.GetString("Graph_Toast_SmartLayoutTitle"),
            LocalizationService.Instance.GetString("Graph_Toast_SmartLayoutSuccess"),
            isSuccess: true,
            durationSeconds: 4);
    }

    private static int GetNodeTypePriority(GraphNodeType type) => type switch
    {
        GraphNodeType.Target => 0,
        GraphNodeType.Subdomain => 1,
        GraphNodeType.Technology => 2,
        GraphNodeType.IpAddress => 3,
        GraphNodeType.NameServer => 4,
        GraphNodeType.MailServer => 5,
        GraphNodeType.Registrar => 6,
        GraphNodeType.EmailVerified => 7,
        GraphNodeType.Contact => 8,
        GraphNodeType.Profile => 9,
        GraphNodeType.Warning => 10,
        _ => 11
    };

    [RelayCommand]
    public void ToggleCollapseNode(GraphNode? node)
    {
        if (node == null) return;

        var visitedDescendants = new HashSet<GraphNode>();
        var queue = new Queue<GraphNode>();
        queue.Enqueue(node);

        var directOutEdges = new Dictionary<GraphNode, List<GraphNode>>();
        foreach (var n in Nodes) directOutEdges[n] = new List<GraphNode>();
        foreach (var e in Edges)
        {
            if (directOutEdges.ContainsKey(e.Source))
            {
                directOutEdges[e.Source].Add(e.Target);
            }
        }

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            foreach (var ch in directOutEdges[curr])
            {
                if (visitedDescendants.Add(ch))
                {
                    queue.Enqueue(ch);
                }
            }
        }

        if (visitedDescendants.Count == 0) return;

        node.IsCollapsed = !node.IsCollapsed;
        node.CollapsedCount = node.IsCollapsed ? visitedDescendants.Count : 0;

        foreach (var desc in visitedDescendants)
        {
            desc.IsVisible = !node.IsCollapsed;
        }

        foreach (var edge in Edges)
        {
            edge.IsVisible = edge.Source.IsVisible && edge.Target.IsVisible;
        }

        OrganizeAndFreezeLayout();
    }

    [RelayCommand]
    public void ResetLayout()
    {
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
        _selectedSession = null;
        _selectedInvestigation = null;
        OnPropertyChanged(nameof(SelectedSession));
        OnPropertyChanged(nameof(SelectedInvestigation));
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
    public void DismissError()
    {
        HasErrorMessage = false;
        _toastService.Dismiss();
    }

    public void ShowToast(string title, string message, bool isSuccess = false, int durationSeconds = 5)
    {
        if (isSuccess)
        {
            _toastService.ShowSuccess(title, message, durationSeconds);
        }
        else
        {
            _toastService.ShowError(title, message, durationSeconds);
        }
    }

    public void ShowErrorToast(string title, string message, int durationSeconds = 5)
    {
        ShowToast(title, message, isSuccess: false, durationSeconds: durationSeconds);
    }

    public static string SanitizeTarget(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        string target = raw.Trim();

        // 1. Strip enclosing brackets, quotes, braces, parens: e.g. [10 silly.haxx.se] -> 10 silly.haxx.se
        target = target.Trim('[', ']', '(', ')', '{', '}', '"', '\'', '`', '<', '>');

        // 2. Strip leading @ for usernames
        target = target.TrimStart('@');

        // 3. Strip DNS MX priority numbers: e.g. "10 silly.haxx.se" -> "silly.haxx.se"
        target = System.Text.RegularExpressions.Regex.Replace(target, @"^\d+\s+", "");

        // 4. Strip URL schemes: http:// or https://
        if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(target, UriKind.Absolute, out var uri))
            {
                target = uri.Host;
            }
        }

        // 5. Strip trailing dot in DNS FQDNs
        target = target.TrimEnd('.');

        return target.Trim();
    }

    private static bool IsPotentiallyValidTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) return false;
        if (target.Length > 255) return false;
        if (target.Any(char.IsWhiteSpace)) return false;
        if (target.IndexOfAny(new[] { '<', '>', ';', '\\', '/', '"', '\'', '^', '`', '{', '}', '|' }) >= 0) return false;

        return true;
    }

    private static string ExtractAndSanitizePivotTarget(GraphNode node)
    {
        string rawTarget = node.Label;

        if (node.Properties.TryGetValue("Хост", out var host) && !string.IsNullOrWhiteSpace(host))
        {
            rawTarget = host;
        }
        else if (node.Properties.TryGetValue("Host", out var h) && !string.IsNullOrWhiteSpace(h))
        {
            rawTarget = h;
        }
        else if (node.Properties.TryGetValue("Username", out var username) && !string.IsNullOrWhiteSpace(username))
        {
            rawTarget = username;
        }
        else if (node.Properties.TryGetValue("Никнейм", out var nick) && !string.IsNullOrWhiteSpace(nick))
        {
            rawTarget = nick;
        }
        else if (node.Properties.TryGetValue("Домен", out var domain) && !string.IsNullOrWhiteSpace(domain))
        {
            rawTarget = domain;
        }
        else if (node.Properties.TryGetValue("Domain", out var d) && !string.IsNullOrWhiteSpace(d))
        {
            rawTarget = d;
        }
        else if (node.Properties.TryGetValue("Email", out var email) && !string.IsNullOrWhiteSpace(email))
        {
            rawTarget = email;
        }

        return SanitizeTarget(rawTarget);
    }

    private static string DetermineTargetType(GraphNode node, string target)
    {
        if (node.Type == GraphNodeType.Contact || target.Contains('@'))
        {
            return "Email";
        }

        if (System.Net.IPAddress.TryParse(target, out _))
        {
            return "Ip";
        }

        if (node.Type == GraphNodeType.Profile)
        {
            return "Auto";
        }

        return "Domain";
    }

    private bool IsAutoExpandCandidate(GraphNode node, string rootDomain)
    {
        if (node.IsExpanded || node.IsExpanding || node.ExpansionDepth >= 1)
        {
            return false;
        }

        string target = ExtractAndSanitizePivotTarget(node);
        if (string.IsNullOrWhiteSpace(target) || !IsPotentiallyValidTarget(target))
        {
            return false;
        }

        if (_visitedTargets.Contains(target))
        {
            return false;
        }

        if (node.Type == GraphNodeType.Contact || target.Contains('@'))
        {
            return true;
        }

        if (node.Type == GraphNodeType.Subdomain)
        {
            if (SupernodeBlacklist.Contains(target))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(rootDomain) &&
                (target.EndsWith("." + rootDomain, StringComparison.OrdinalIgnoreCase) ||
                 target.Equals(rootDomain, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    public async Task RunSmartAutoExpandAsync()
    {
        if (IsPivoting) return;

        string rootDomain = TargetDisplay != "—" ? TargetDisplay : _searchViewModel.ActiveTarget;
        rootDomain = SanitizeTarget(rootDomain);

        var candidates = Nodes.Where(n => IsAutoExpandCandidate(n, rootDomain)).Take(5).ToList();
        if (candidates.Count == 0) return;

        ShowErrorToast(
            LocalizationService.Instance.GetString("Graph_AutoExpand_On"),
            string.Format(LocalizationService.Instance.GetString("Graph_Toast_AutoExpandStarted"), candidates.Count));

        foreach (var candidate in candidates)
        {
            if (Nodes.Count >= MaxCanvasNodes)
            {
                ShowErrorToast(
                    LocalizationService.Instance.GetString("Graph_Toast_LimitTitle"),
                    LocalizationService.Instance.GetString("Graph_Toast_LimitReached"));
                break;
            }

            await ExpandNodeAsync(candidate);
        }
    }

    [RelayCommand]
    public Task PivotSearchOnNodeAsync() => ExpandNodeAsync(SelectedNode);

    [RelayCommand]
    public async Task ExpandNodeAsync(GraphNode? targetNode = null)
    {
        var node = targetNode ?? SelectedNode;
        if (node == null || IsPivoting || node.IsExpanding) return;

        if (Nodes.Count >= MaxCanvasNodes)
        {
            ShowErrorToast(
                LocalizationService.Instance.GetString("Graph_Toast_LimitTitle"),
                LocalizationService.Instance.GetString("Graph_Toast_LimitReached"));
            return;
        }

        string pivotTarget = ExtractAndSanitizePivotTarget(node);

        if (!IsPotentiallyValidTarget(pivotTarget))
        {
            string errTitle = LocalizationService.Instance.GetString("Graph_Toast_PivotErrorTitle");
            string errMsg = string.Format(LocalizationService.Instance.GetString("Graph_Toast_InvalidTarget"), node.Label);
            ShowErrorToast(errTitle, errMsg);
            _logService.LogWarning("Pivot", $"Попытка экспанда узла с недопустимым значением: [{node.Label}] -> [{pivotTarget}]");
            return;
        }

        if (_visitedTargets.Contains(pivotTarget))
        {
            node.IsExpanded = true;
            string targetNodeId = $"target:{pivotTarget.ToLowerInvariant().Trim()}";
            if (_nodeMap.TryGetValue(targetNodeId, out var existingTarget) && existingTarget != node)
            {
                string bridgeEdgeId = $"{node.Id}->{existingTarget.Id}:Pivot";
                if (!Edges.Any(e => e.Id == bridgeEdgeId))
                {
                    Edges.Add(new GraphEdge(node, existingTarget, "Связь (Кэш)"));
                }
            }
            ShowErrorToast(
                LocalizationService.Instance.GetString("Graph_Toast_PivotErrorTitle"),
                string.Format(LocalizationService.Instance.GetString("Graph_Toast_AlreadyVisited"), pivotTarget));
            return;
        }

        _visitedTargets.Add(pivotTarget);

        CancelPivot();
        _pivotCts = new CancellationTokenSource();
        var ct = _pivotCts.Token;

        node.IsExpanding = true;
        IsPivoting = true;
        PivotingTarget = pivotTarget;
        PivotFindingsCount = 0;

        _logService.LogInfo("Pivot", $"Запуск экспанда узла [{pivotTarget}] (тип: {node.TypeNameRu})");
        _toastService.ShowInfo("Экспанд узла", $"Запущен поиск связей для «{pivotTarget}»");

        try
        {
            EnsureCurrentInvestigationExists();

            string targetType = DetermineTargetType(node, pivotTarget);
            var sessionResponse = await _apiClient.CreateSessionAsync(pivotTarget, targetType, ct);
            string sessionId = sessionResponse.EffectiveId;

            if (CurrentInvestigation != null && !CurrentInvestigation.SessionIds.Contains(sessionId))
            {
                CurrentInvestigation.SessionIds.Add(sessionId);
            }
            if (!CurrentInvestigationTargets.Contains(pivotTarget))
            {
                CurrentInvestigationTargets.Add(pivotTarget);
            }

            var stream = _streamClient.SubscribeToSessionAsync(sessionId, ct);

            await foreach (var message in stream.WithCancellation(ct))
            {
                if (Nodes.Count >= MaxCanvasNodes)
                {
                    ShowErrorToast(
                        LocalizationService.Instance.GetString("Graph_Toast_LimitTitle"),
                        LocalizationService.Instance.GetString("Graph_Toast_LimitReached"));
                    break;
                }

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
                        bool mergeShared = _settingsService.Settings.MergeSharedGraphEntities;
                        GraphDataMapper.MapFindingToGraph(findingVm, pivotTarget, _nodeMap, newEdges, _random, node.X, node.Y, mergeShared);

                        foreach (var n in _nodeMap.Values)
                        {
                            if (!Nodes.Contains(n))
                            {
                                n.ExpansionDepth = node.ExpansionDepth + 1;
                                Nodes.Add(n);
                            }
                        }

                        string targetNodeId = $"target:{pivotTarget.ToLowerInvariant().Trim()}";
                        if (_nodeMap.TryGetValue(targetNodeId, out var newTargetNode) && newTargetNode != node)
                        {
                            string bridgeEdgeId = $"{node.Id}->{newTargetNode.Id}:Pivot";
                            if (!Edges.Any(e => e.Id == bridgeEdgeId))
                            {
                                Edges.Add(new GraphEdge(node, newTargetNode, "Pivot связь"));
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

            _logService.LogInfo("Pivot", $"Экспанд узла [{pivotTarget}] завершен. Находок: {PivotFindingsCount}");
            _toastService.ShowSuccess("Узел развёрнут", $"Для «{pivotTarget}» найдено связей: {PivotFindingsCount}");
        }
        catch (OperationCanceledException)
        {
            _logService.LogWarning("Pivot", $"Экспанд [{pivotTarget}] отменен пользователем.");
        }
        catch (System.Net.Http.HttpRequestException httpEx)
        {
            _logService.LogError("Pivot", $"Ошибка экспанда узла [{pivotTarget}]: {httpEx.Message}", httpEx);

            string errTitle = LocalizationService.Instance.GetString("Graph_Toast_PivotErrorTitle");
            string errMsg = (httpEx.StatusCode == System.Net.HttpStatusCode.BadRequest || httpEx.Message.Contains("400") || httpEx.Message.Contains("invalid"))
                ? string.Format(LocalizationService.Instance.GetString("Graph_Toast_BadRequest"), pivotTarget)
                : string.Format(LocalizationService.Instance.GetString("Graph_Toast_GenericError"), pivotTarget, httpEx.Message);

            ShowErrorToast(errTitle, errMsg);
        }
        catch (Exception ex)
        {
            _logService.LogError("Pivot", $"Ошибка экспанда узла [{pivotTarget}]: {ex.Message}", ex);

            string errTitle = LocalizationService.Instance.GetString("Graph_Toast_PivotErrorTitle");
            string errMsg = string.Format(LocalizationService.Instance.GetString("Graph_Toast_GenericError"), pivotTarget, ex.Message);
            ShowErrorToast(errTitle, errMsg);
        }
        finally
        {
            node.IsExpanding = false;
            node.IsExpanded = true;
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
        void Update()
        {
            var currentId = SelectedInvestigation?.Id;
            var list = _investigationService.GetAll();
            AvailableInvestigations.Clear();
            foreach (var item in list)
            {
                AvailableInvestigations.Add(item);
            }

            if (!string.IsNullOrEmpty(currentId))
            {
                _selectedInvestigation = AvailableInvestigations.FirstOrDefault(i => i.Id == currentId);
                OnPropertyChanged(nameof(SelectedInvestigation));
            }
        }

        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(Update);
        }
        else
        {
            Update();
        }
    }

    public void EnsureCurrentInvestigationExists()
    {
        string target = !string.IsNullOrWhiteSpace(TargetDisplay) && TargetDisplay != "—"
            ? TargetDisplay
            : "Новое расследование";

        if (CurrentInvestigation == null)
        {
            var existing = !string.IsNullOrWhiteSpace(_searchViewModel.CurrentSessionId)
                ? _investigationService.GetAll().FirstOrDefault(i => i.SessionIds.Contains(_searchViewModel.CurrentSessionId))
                : _investigationService.GetAll().FirstOrDefault(i => string.Equals(i.RootTarget, target, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                CurrentInvestigation = existing;
            }
            else
            {
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
            }
        }

        bool updated = false;
        foreach (var finding in _searchViewModel.Findings)
        {
            if (!CurrentInvestigation.CachedFindings.Any(f => f.EffectiveId == finding.FindingId))
            {
                CurrentInvestigation.CachedFindings.Add(finding.Model);
                updated = true;
            }
        }

        if (updated || !_investigationService.GetAll().Any(i => i.Id == CurrentInvestigation.Id))
        {
            _investigationService.Save(CurrentInvestigation);
            ReloadAvailableInvestigations();
        }
    }

    public async Task LoadInvestigationAsync(InvestigationModel inv)
    {
        IsLoadingSession = true;
        IsLiveMode = false;
        IsInvestigationMode = true;
        _selectedSession = null;
        OnPropertyChanged(nameof(SelectedSession));
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

    public event Action? RequestExportHighDpiImage;

    [RelayCommand]
    public void ExportGraphImage()
    {
        RequestExportHighDpiImage?.Invoke();
    }

    [RelayCommand]
    public void ExportMarkdownReport()
    {
        EnsureCurrentInvestigationExists();
        string title = CurrentInvestigation?.Title ?? TargetDisplay;
        var sfd = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Экспорт отчёта расследования (Markdown)",
            Filter = "Markdown отчёт (*.md)|*.md|Все файлы (*.*)|*.*",
            FileName = $"Investigation_Report_{SanitizeFileName(title)}_{DateTime.Now:yyyyMMdd_HHmm}.md"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"# OSINT Расследование: {title}");
                sb.AppendLine();
                sb.AppendLine($"- **Дата генерации:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"- **Цель:** {TargetDisplay}");
                sb.AppendLine($"- **Всего узлов в графе:** {Nodes.Count}");
                sb.AppendLine($"- **Всего связей:** {Edges.Count}");
                sb.AppendLine();
                sb.AppendLine("## 1. Сводка по категориям");
                sb.AppendLine();

                var grouped = Nodes.GroupBy(n => n.Type).OrderBy(g => GetNodeTypePriority(g.Key));
                foreach (var grp in grouped)
                {
                    sb.AppendLine($"### {grp.First().TypeNameRu} ({grp.Count()})");
                    foreach (var n in grp.OrderBy(x => x.Label))
                    {
                        sb.AppendLine($"- **{n.Label}** — {n.SubLabel}");
                        if (n.Properties.Count > 0)
                        {
                            foreach (var (k, v) in n.Properties)
                            {
                                if (!string.IsNullOrWhiteSpace(v))
                                    sb.AppendLine($"  - *{k}*: {v}");
                            }
                        }
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("## 2. Карта связей");
                sb.AppendLine();
                sb.AppendLine("| Источник | Связь | Назначение |");
                sb.AppendLine("| :--- | :---: | :--- |");
                foreach (var edge in Edges)
                {
                    sb.AppendLine($"| {edge.Source.Label} | {edge.Label} | {edge.Target.Label} |");
                }

                System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                _toastService.ShowSuccess("Экспорт отчёта", $"Отчёт успешно сохранён: {System.IO.Path.GetFileName(sfd.FileName)}");
            }
            catch (Exception ex)
            {
                _logService.LogError("Report", $"Ошибка при создании Markdown отчёта: {ex.Message}", ex);
                ShowErrorToast("Ошибка экспорта", ex.Message);
            }
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
