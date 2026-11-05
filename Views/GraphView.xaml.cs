using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using MeridianClient.Models;
using MeridianClient.Services;
using MeridianClient.ViewModels;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace MeridianClient.Views;

public partial class GraphView : UserControl
{
    private readonly Dictionary<GraphNode, FrameworkElement> _nodeVisuals = new();
    private readonly Dictionary<string, DateTime> _edgeSpawnTimes = new();
    private readonly Canvas _edgeCanvas = new();
    private readonly Canvas _nodesContainer = new();

    private bool _isPanning;
    private Point _panStartPoint;
    private Matrix _panStartMatrix;

    private GraphNode? _draggedNode;
    private Point _nodeDragOffset;

    private Pen _edgePenNormal = new(new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)), 1.2);
    private Pen _edgePenHighlighted = new(new SolidColorBrush(Color.FromArgb(200, 6, 182, 212)), 2.2);

    public GraphView()
    {
        InitializeComponent();

        _edgePenNormal.Freeze();
        _edgePenHighlighted.Freeze();

        // Assemble layered canvas: Edges below, Nodes on top
        graphCanvas.Children.Add(_edgeCanvas);
        graphCanvas.Children.Add(_nodesContainer);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is GraphViewModel vm)
        {
            vm.RequestCenterView -= CenterGraphOnScreen;
            vm.RequestCenterView += CenterGraphOnScreen;

            vm.RequestExportHighDpiImage -= ExportHighDpiImage;
            vm.RequestExportHighDpiImage += ExportHighDpiImage;

            ((INotifyCollectionChanged)vm.Nodes).CollectionChanged -= OnNodesCollectionChanged;
            ((INotifyCollectionChanged)vm.Nodes).CollectionChanged += OnNodesCollectionChanged;

            ((INotifyCollectionChanged)vm.Edges).CollectionChanged -= OnEdgesCollectionChanged;
            ((INotifyCollectionChanged)vm.Edges).CollectionChanged += OnEdgesCollectionChanged;

            RebuildAllVisuals(animate: true);

            CompositionTarget.Rendering -= OnRenderFrame;
            CompositionTarget.Rendering += OnRenderFrame;

            Dispatcher.InvokeAsync(CenterGraphOnScreen, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering -= OnRenderFrame;
        if (DataContext is GraphViewModel vm)
        {
            vm.RequestCenterView -= CenterGraphOnScreen;
            vm.RequestExportHighDpiImage -= ExportHighDpiImage;
            ((INotifyCollectionChanged)vm.Nodes).CollectionChanged -= OnNodesCollectionChanged;
            ((INotifyCollectionChanged)vm.Edges).CollectionChanged -= OnEdgesCollectionChanged;
        }
    }

    private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _nodesContainer.Children.Clear();
            _nodeVisuals.Clear();
            _edgeSpawnTimes.Clear();
            _edgeCanvas.Children.Clear();
            return;
        }

        if (e.OldItems != null)
        {
            foreach (GraphNode node in e.OldItems)
            {
                if (_nodeVisuals.TryGetValue(node, out var visual))
                {
                    _nodesContainer.Children.Remove(visual);
                    _nodeVisuals.Remove(node);
                }
            }
        }

        if (e.NewItems != null)
        {
            int delay = 0;
            foreach (GraphNode node in e.NewItems)
            {
                CreateVisualForNode(node, animate: true, delayMs: delay);
                delay = Math.Min(delay + 35, 300);
            }
        }
    }

    private void OnEdgesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Edges are redrawn every frame in OnRenderFrame
    }

    private void RebuildAllVisuals(bool animate = false)
    {
        _nodesContainer.Children.Clear();
        _nodeVisuals.Clear();
        _edgeSpawnTimes.Clear();

        if (DataContext is GraphViewModel vm)
        {
            int index = 0;
            foreach (var node in vm.Nodes)
            {
                int delay = animate ? Math.Min(index * 25, 450) : 0;
                CreateVisualForNode(node, animate: animate, delayMs: delay);
                index++;
            }
        }
    }

    private const double NodeContainerWidth = 160.0;

    private void CreateVisualForNode(GraphNode node, bool animate = false, int delayMs = 0)
    {
        if (_nodeVisuals.ContainsKey(node)) return;

        double diameter = node.Radius * 2.0;

        var container = new StackPanel
        {
            Width = NodeContainerWidth,
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            Cursor = Cursors.Hand,
            ToolTip = $"{node.TypeNameRu}: {node.Label}"
        };

        // Node Circle / Badge
        var cardBrush = Application.Current.FindResource("CardBackgroundBrush") as Brush
                        ?? new SolidColorBrush(Color.FromRgb(17, 24, 39));

        var circle = new Border
        {
            Width = diameter,
            Height = diameter,
            CornerRadius = new CornerRadius(node.Radius),
            Background = cardBrush,
            BorderBrush = new SolidColorBrush(node.AccentColor),
            BorderThickness = new Thickness(node.Type == GraphNodeType.Target ? 2.5 : 1.5),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        if (node.Type == GraphNodeType.Target && !animate)
        {
            circle.Effect = new DropShadowEffect
            {
                Color = node.AccentColor,
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.85
            };
        }

        var symbol = Enum.TryParse<SymbolRegular>(node.IconSymbol, out var parsedSymbol)
            ? parsedSymbol
            : SymbolRegular.Circle24;

        var icon = new SymbolIcon
        {
            Symbol = symbol,
            FontSize = node.Radius * 1.05,
            Foreground = new SolidColorBrush(node.AccentColor),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        circle.Child = icon;

        var circleGrid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        circleGrid.Children.Add(circle);

        var collapseBadge = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(230, 245, 158, 11)),
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(4, 0, 4, 1),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -4, -6, 0),
            Cursor = Cursors.Hand,
            Visibility = node.IsCollapsed ? Visibility.Visible : Visibility.Collapsed,
            ToolTip = "Нажмите, чтобы развернуть ветвь"
        };
        var badgeText = new TextBlock
        {
            Text = $"+{node.CollapsedCount}",
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.Black,
            FontFamily = new FontFamily("JetBrains Mono, Consolas")
        };
        collapseBadge.Child = badgeText;
        collapseBadge.MouseLeftButtonDown += (s, e) =>
        {
            if (DataContext is GraphViewModel vm)
            {
                vm.ToggleCollapseNodeCommand.Execute(node);
                e.Handled = true;
            }
        };
        circleGrid.Children.Add(collapseBadge);

        container.Children.Add(circleGrid);

        // Node Label underneath (centered under circle)
        var label = new TextBlock
        {
            Text = node.Label,
            FontSize = node.Type == GraphNodeType.Target ? 12 : 10,
            FontWeight = node.Type == GraphNodeType.Target ? FontWeights.Bold : FontWeights.SemiBold,
            Foreground = Application.Current.FindResource("PrimaryTextBrush") as Brush ?? Brushes.White,
            FontFamily = new FontFamily("JetBrains Mono, Cascadia Code, Consolas"),
            TextAlignment = TextAlignment.Center,
            MaxWidth = NodeContainerWidth - 10,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 4, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        container.Children.Add(label);

        // Handle clicks and dragging
        container.MouseLeftButtonDown += (s, e) =>
        {
            Focus();

            if (DataContext is GraphViewModel vm)
            {
                vm.SelectNode(node);

                if (e.ClickCount == 2 && vm.PivotSearchOnNodeCommand.CanExecute(null))
                {
                    vm.PivotSearchOnNodeCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
            }

            _draggedNode = node;
            var pos = e.GetPosition(graphCanvas);
            _nodeDragOffset = new Point(pos.X - node.X, pos.Y - node.Y);
            container.CaptureMouse();
            e.Handled = true;
        };

        container.MouseMove += (s, e) =>
        {
            if (_draggedNode == node)
            {
                var pos = e.GetPosition(graphCanvas);
                node.X = pos.X - _nodeDragOffset.X;
                node.Y = pos.Y - _nodeDragOffset.Y;
                node.Vx = 0;
                node.Vy = 0;
            }
        };

        container.MouseLeftButtonUp += (s, e) =>
        {
            if (_draggedNode == node)
            {
                _draggedNode = null;
                container.ReleaseMouseCapture();
                e.Handled = true;
            }
        };

        var contextMenu = new ContextMenu();
        var expandMenuItem = new MenuItem
        {
            Header = LocalizationService.Instance.GetString("Graph_Btn_ExpandNode"),
            Icon = new SymbolIcon { Symbol = SymbolRegular.Flash24, FontSize = 14 }
        };
        expandMenuItem.Click += (s, e) =>
        {
            if (DataContext is GraphViewModel vm && vm.ExpandNodeCommand.CanExecute(node))
            {
                vm.ExpandNodeCommand.Execute(node);
            }
        };

        var searchMenuItem = new MenuItem
        {
            Header = LocalizationService.Instance.GetString("Search_StartButton"),
            Icon = new SymbolIcon { Symbol = SymbolRegular.Search24, FontSize = 14 }
        };
        searchMenuItem.Click += (s, e) =>
        {
            if (DataContext is GraphViewModel vm)
            {
                vm.SelectNode(node);
                if (vm.StartSearchOnNodeCommand.CanExecute(null))
                {
                    vm.StartSearchOnNodeCommand.Execute(null);
                }
            }
        };

        var copyMenuItem = new MenuItem
        {
            Header = LocalizationService.Instance.GetString("Search_Card_CopyJson"),
            Icon = new SymbolIcon { Symbol = SymbolRegular.Copy24, FontSize = 14 }
        };
        copyMenuItem.Click += (s, e) =>
        {
            try
            {
                Clipboard.SetText(node.Label);
            }
            catch { }
        };

        var toggleCollapseItem = new MenuItem
        {
            Header = node.IsCollapsed ? "Развернуть ветвь" : "Свернуть ветвь",
            Icon = new SymbolIcon { Symbol = SymbolRegular.BranchFork24, FontSize = 14 }
        };
        toggleCollapseItem.Click += (s, e) =>
        {
            if (DataContext is GraphViewModel vm)
            {
                vm.ToggleCollapseNodeCommand.Execute(node);
            }
        };

        contextMenu.Opened += (s, e) =>
        {
            toggleCollapseItem.Header = node.IsCollapsed ? "Развернуть ветвь" : "Свернуть ветвь";
            if (node.IsExpanding)
            {
                expandMenuItem.IsEnabled = false;
                expandMenuItem.Header = LocalizationService.Instance.GetString("Graph_Btn_NodeExpanding");
            }
            else if (node.IsExpanded)
            {
                expandMenuItem.IsEnabled = false;
                expandMenuItem.Header = LocalizationService.Instance.GetString("Graph_Btn_NodeExpanded");
            }
            else
            {
                expandMenuItem.IsEnabled = true;
                expandMenuItem.Header = LocalizationService.Instance.GetString("Graph_Btn_ExpandNode");
            }
        };

        contextMenu.Items.Add(expandMenuItem);
        contextMenu.Items.Add(toggleCollapseItem);
        contextMenu.Items.Add(searchMenuItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(copyMenuItem);

        container.ContextMenu = contextMenu;

        _nodeVisuals[node] = container;
        _nodesContainer.Children.Add(container);

        Canvas.SetLeft(container, node.X - NodeContainerWidth / 2.0);
        Canvas.SetTop(container, node.Y - node.Radius);

        if (animate)
        {
            ApplySpawnAnimation(container, circle, node, delayMs);
        }
    }

    private void ApplySpawnAnimation(StackPanel container, Border circle, GraphNode node, int delayMs)
    {
        var scale = new ScaleTransform(0.05, 0.05);
        container.RenderTransformOrigin = new Point(0.5, 0.28);
        container.RenderTransform = scale;
        container.Opacity = 0.0;

        var backEase = new BackEase
        {
            EasingMode = EasingMode.EaseOut,
            Amplitude = 0.45
        };
        var cubicEase = new CubicEase
        {
            EasingMode = EasingMode.EaseOut
        };

        var scaleXAnim = new DoubleAnimation(0.05, 1.0, TimeSpan.FromMilliseconds(480))
        {
            EasingFunction = backEase
        };
        var scaleYAnim = new DoubleAnimation(0.05, 1.0, TimeSpan.FromMilliseconds(480))
        {
            EasingFunction = backEase
        };
        var opacityAnim = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(380))
        {
            EasingFunction = cubicEase
        };

        // Shockwave neon glow pulse
        var spawnGlow = new DropShadowEffect
        {
            Color = node.AccentColor,
            BlurRadius = 38,
            ShadowDepth = 0,
            Opacity = 1.0
        };
        circle.Effect = spawnGlow;

        double targetBlur = node.Type == GraphNodeType.Target ? 18.0 : 0.0;
        double targetGlowOpacity = node.Type == GraphNodeType.Target ? 0.85 : 0.0;

        var blurAnim = new DoubleAnimation(38, targetBlur, TimeSpan.FromMilliseconds(650))
        {
            EasingFunction = cubicEase
        };
        var glowOpacityAnim = new DoubleAnimation(1.0, targetGlowOpacity, TimeSpan.FromMilliseconds(650))
        {
            EasingFunction = cubicEase
        };

        if (delayMs > 0)
        {
            var beginTime = TimeSpan.FromMilliseconds(delayMs);
            scaleXAnim.BeginTime = beginTime;
            scaleYAnim.BeginTime = beginTime;
            opacityAnim.BeginTime = beginTime;
            blurAnim.BeginTime = beginTime;
            glowOpacityAnim.BeginTime = beginTime;
        }

        glowOpacityAnim.Completed += (s, e) =>
        {
            if (node.Type != GraphNodeType.Target && !node.IsSelected && !node.IsHighlighted)
            {
                circle.Effect = null;
            }
        };

        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
        container.BeginAnimation(UIElement.OpacityProperty, opacityAnim);

        spawnGlow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnim);
        spawnGlow.BeginAnimation(DropShadowEffect.OpacityProperty, glowOpacityAnim);
    }

    private void OnRenderFrame(object? sender, EventArgs e)
    {
        if (DataContext is not GraphViewModel vm) return;

        // 1. Update node visual positions on canvas (perfect center tracking)
        foreach (var node in vm.Nodes)
        {
            if (_nodeVisuals.TryGetValue(node, out var visual))
            {
                visual.Visibility = node.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                if (!node.IsVisible) continue;

                Canvas.SetLeft(visual, node.X - NodeContainerWidth / 2.0);
                Canvas.SetTop(visual, node.Y - node.Radius);

                if (visual is StackPanel panel && panel.Children[0] is Grid circleGrid)
                {
                    Border? border = circleGrid.Children.Count > 0 ? circleGrid.Children[0] as Border : null;
                    Border? badge = circleGrid.Children.Count > 1 ? circleGrid.Children[1] as Border : null;

                    if (badge != null)
                    {
                        badge.Visibility = node.IsCollapsed ? Visibility.Visible : Visibility.Collapsed;
                        if (badge.Child is TextBlock tb)
                        {
                            tb.Text = $"+{node.CollapsedCount}";
                        }
                    }

                    if (border != null)
                    {
                    if (node.IsExpanding)
                    {
                        double pulse = (Math.Sin(DateTime.UtcNow.TimeOfDay.TotalSeconds * 7.0) + 1.0) / 2.0;
                        border.BorderThickness = new Thickness(2.0 + pulse * 1.5);
                        border.Effect = new DropShadowEffect
                        {
                            Color = node.AccentColor,
                            BlurRadius = 16.0 + pulse * 20.0,
                            ShadowDepth = 0,
                            Opacity = 0.85 + pulse * 0.15
                        };
                    }
                    else if (node.IsSelected)
                    {
                        border.BorderThickness = new Thickness(3.0);
                        border.Effect = new DropShadowEffect
                        {
                            Color = node.AccentColor,
                            BlurRadius = 22,
                            ShadowDepth = 0,
                            Opacity = 1.0
                        };
                    }
                    else if (node.IsHighlighted)
                    {
                        border.BorderThickness = new Thickness(2.0);
                        border.Effect = new DropShadowEffect
                        {
                            Color = node.AccentColor,
                            BlurRadius = 14,
                            ShadowDepth = 0,
                            Opacity = 0.75
                        };
                    }
                    else if (node.IsThreat)
                    {
                        double pulse = (Math.Sin(DateTime.UtcNow.TimeOfDay.TotalSeconds * 5.0) + 1.0) / 2.0;
                        border.BorderThickness = new Thickness(2.0 + pulse * 1.5);
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 51, 102));
                        border.Effect = new DropShadowEffect
                        {
                            Color = Color.FromRgb(255, 51, 102),
                            BlurRadius = 14.0 + pulse * 16.0,
                            ShadowDepth = 0,
                            Opacity = 0.8 + pulse * 0.2
                        };
                    }
                    else if (node.Type != GraphNodeType.Target)
                    {
                        if (border.Effect is DropShadowEffect effect && effect.HasAnimatedProperties)
                        {
                        }
                        else
                        {
                            border.BorderThickness = new Thickness(node.IsExpanded ? 2.0 : 1.5);
                            border.Effect = null;
                        }
                    }
                }
            }
        }
        }

        // 2. Redraw edges
        DrawEdges(vm.Edges);
    }

    private void DrawEdges(IReadOnlyList<GraphEdge> edges)
    {
        _edgeCanvas.Children.Clear();

        var brushAccent = Application.Current.FindResource("AccentBrush") as SolidColorBrush ?? Brushes.Cyan;
        var brushThreat = new SolidColorBrush(Color.FromRgb(255, 51, 102));
        var edgePenActive = new Pen(brushAccent, 2.0);
        var edgePenThreat = new Pen(brushThreat, 2.5);
        var now = DateTime.UtcNow;

        foreach (var edge in edges)
        {
            if (!edge.IsVisible || !edge.Source.IsVisible || !edge.Target.IsVisible) continue;
            if (!_edgeSpawnTimes.TryGetValue(edge.Id, out var spawnTime))
            {
                spawnTime = now;
                _edgeSpawnTimes[edge.Id] = spawnTime;
            }

            double ageMs = (now - spawnTime).TotalMilliseconds;
            double fadeProgress = Math.Clamp(ageMs / 350.0, 0.05, 1.0);

            Brush strokeBrush = _edgePenNormal.Brush;
            double thickness = 1.2;
            double opacity = 0.45;

            if (edge.IsThreatPath)
            {
                strokeBrush = edgePenThreat.Brush;
                thickness = 2.5;
                opacity = 1.0;
            }
            else if (edge.IsHighlighted)
            {
                strokeBrush = edgePenActive.Brush;
                thickness = 2.0;
                opacity = 0.95;
            }

            var line = new Line
            {
                X1 = edge.Source.X,
                Y1 = edge.Source.Y,
                X2 = edge.Target.X,
                Y2 = edge.Target.Y,
                Stroke = strokeBrush,
                StrokeThickness = thickness,
                Opacity = opacity * fadeProgress
            };
            _edgeCanvas.Children.Add(line);
        }
    }

    #region Zoom & Pan Viewport Handling

    private void CanvasViewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var mousePos = e.GetPosition(canvasViewport);
        double zoomFactor = e.Delta > 0 ? 1.15 : 1 / 1.15;

        var matrix = canvasTransform.Matrix;

        // Restrict zoom limits (0.15x to 5.0x)
        if ((matrix.M11 * zoomFactor < 0.15 && e.Delta < 0) || (matrix.M11 * zoomFactor > 5.0 && e.Delta > 0))
            return;

        matrix.ScaleAt(zoomFactor, zoomFactor, mousePos.X, mousePos.Y);
        canvasTransform.Matrix = matrix;
        e.Handled = true;
    }

    private void CanvasViewport_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Focus();

        if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Middle)
        {
            _isPanning = true;
            _panStartPoint = e.GetPosition(canvasViewport);
            _panStartMatrix = canvasTransform.Matrix;
            canvasViewport.CaptureMouse();
            canvasViewport.Cursor = Cursors.SizeAll;
        }
    }

    private void CanvasViewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isPanning)
        {
            var currentPos = e.GetPosition(canvasViewport);
            var delta = currentPos - _panStartPoint;

            var matrix = _panStartMatrix;
            matrix.Translate(delta.X, delta.Y);
            canvasTransform.Matrix = matrix;
        }
    }

    private void CanvasViewport_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            canvasViewport.ReleaseMouseCapture();
            canvasViewport.Cursor = Cursors.Arrow;
        }
    }

    private void BtnCenterGraph_Click(object sender, RoutedEventArgs e)
    {
        CenterGraphOnScreen();
    }

    public void CenterGraphOnScreen()
    {
        if (DataContext is not GraphViewModel vm || vm.Nodes.Count == 0)
        {
            // Reset to identity centered
            var m = Matrix.Identity;
            m.Translate(canvasViewport.ActualWidth / 2 - 400, canvasViewport.ActualHeight / 2 - 300);
            canvasTransform.Matrix = m;
            return;
        }

        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;

        foreach (var node in vm.Nodes)
        {
            if (node.X < minX) minX = node.X;
            if (node.X > maxX) maxX = node.X;
            if (node.Y < minY) minY = node.Y;
            if (node.Y > maxY) maxY = node.Y;
        }

        double graphWidth = Math.Max(maxX - minX + 160, 200);
        double graphHeight = Math.Max(maxY - minY + 160, 200);

        double viewWidth = canvasViewport.ActualWidth > 100 ? canvasViewport.ActualWidth : 800;
        double viewHeight = canvasViewport.ActualHeight > 100 ? canvasViewport.ActualHeight : 600;

        double scaleX = viewWidth / graphWidth;
        double scaleY = viewHeight / graphHeight;
        double scale = Math.Clamp(Math.Min(scaleX, scaleY), 0.35, 1.3);

        double centerX = (minX + maxX) / 2.0;
        double centerY = (minY + maxY) / 2.0;

        var matrix = Matrix.Identity;
        matrix.Scale(scale, scale);
        matrix.Translate(viewWidth / 2.0 - centerX * scale, viewHeight / 2.0 - centerY * scale);

        canvasTransform.Matrix = matrix;
    }

    #endregion

    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (e.OriginalSource == nodeSearchTextBox)
            {
                return;
            }

            if (DataContext is GraphViewModel vm && vm.SelectedNode != null && vm.PivotSearchOnNodeCommand.CanExecute(null))
            {
                vm.PivotSearchOnNodeCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void NodeSearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is GraphViewModel vm)
            {
                if (vm.SelectedNode != null && vm.PivotSearchOnNodeCommand.CanExecute(null))
                {
                    vm.PivotSearchOnNodeCommand.Execute(null);
                    Focus();
                    e.Handled = true;
                }
                else
                {
                    var match = vm.Nodes.FirstOrDefault(n => n.IsHighlighted)
                                ?? vm.Nodes.FirstOrDefault(n =>
                                    !string.IsNullOrWhiteSpace(vm.NodeSearchText) &&
                                    (n.Label.Contains(vm.NodeSearchText, StringComparison.OrdinalIgnoreCase) ||
                                     n.SubLabel.Contains(vm.NodeSearchText, StringComparison.OrdinalIgnoreCase)));
                    if (match != null)
                    {
                        vm.SelectNode(match);
                        Focus();
                        e.Handled = true;
                    }
                }
            }
        }
    }

    private void ExportHighDpiImage()
    {
        if (DataContext is not GraphViewModel vm || vm.Nodes.Count == 0) return;

        var visibleNodes = vm.Nodes.Where(n => n.IsVisible).ToList();
        if (visibleNodes.Count == 0) return;

        double minX = visibleNodes.Min(n => n.X) - NodeContainerWidth;
        double maxX = visibleNodes.Max(n => n.X) + NodeContainerWidth;
        double minY = visibleNodes.Min(n => n.Y) - 80;
        double maxY = visibleNodes.Max(n => n.Y) + 120;

        double padding = 80;
        minX -= padding;
        minY -= padding;
        maxX += padding;
        maxY += padding;

        double contentWidth = Math.Max(400, maxX - minX);
        double contentHeight = Math.Max(300, maxY - minY);

        var sfd = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Экспорт снимка графа (High-DPI PNG)",
            Filter = "PNG Изображение (*.png)|*.png|Все файлы (*.*)|*.*",
            FileName = $"Meridian_Graph_{DateTime.Now:yyyyMMdd_HHmm}.png"
        };

        if (sfd.ShowDialog() != true) return;

        try
        {
            double scaleFactor = 2.0;
            int pixelWidth = (int)Math.Ceiling(contentWidth * scaleFactor);
            int pixelHeight = (int)Math.Ceiling(contentHeight * scaleFactor);

            var visual = new DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                var bgBrush = Application.Current.FindResource("AppBackgroundBrush") as Brush
                              ?? new SolidColorBrush(Color.FromRgb(24, 26, 32));
                ctx.DrawRectangle(bgBrush, null, new Rect(0, 0, contentWidth, contentHeight));

                ctx.PushTransform(new TranslateTransform(-minX, -minY));

                var brushAccent = Application.Current.FindResource("AccentBrush") as SolidColorBrush ?? Brushes.Cyan;
                var edgePenActive = new Pen(brushAccent, 2.0);

                foreach (var edge in vm.Edges)
                {
                    if (!edge.IsVisible || !edge.Source.IsVisible || !edge.Target.IsVisible) continue;
                    var pen = edge.IsHighlighted ? edgePenActive : _edgePenNormal;
                    ctx.DrawLine(pen, new Point(edge.Source.X, edge.Source.Y), new Point(edge.Target.X, edge.Target.Y));
                }

                var cardBrush = Application.Current.FindResource("CardBackgroundBrush") as Brush
                                ?? new SolidColorBrush(Color.FromRgb(32, 36, 44));
                var primaryTextBrush = Application.Current.FindResource("PrimaryTextBrush") as Brush ?? Brushes.White;
                var font = new Typeface(new FontFamily("JetBrains Mono, Cascadia Code, Consolas"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

                foreach (var node in visibleNodes)
                {
                    var center = new Point(node.X, node.Y);
                    var nodeBorderPen = new Pen(new SolidColorBrush(node.AccentColor), node.Type == GraphNodeType.Target ? 2.5 : 1.5);
                    ctx.DrawEllipse(cardBrush, nodeBorderPen, center, node.Radius, node.Radius);

                    var ft = new FormattedText(
                        node.Label,
                        System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        font,
                        node.Type == GraphNodeType.Target ? 12 : 10,
                        primaryTextBrush,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    double textX = node.X - ft.Width / 2.0;
                    double textY = node.Y + node.Radius + 4;
                    ctx.DrawText(ft, new Point(textX, textY));
                }

                ctx.Pop();
            }

            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                pixelWidth,
                pixelHeight,
                96 * scaleFactor,
                96 * scaleFactor,
                PixelFormats.Pbgra32);
            rtb.Render(visual);

            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));

            using var fileStream = System.IO.File.Create(sfd.FileName);
            encoder.Save(fileStream);

            vm.ShowToast("Снимок сохранён", $"Граф успешно экспортирован в {System.IO.Path.GetFileName(sfd.FileName)}", isSuccess: true);
        }
        catch (Exception ex)
        {
            vm.ShowErrorToast("Ошибка экспорта снимка", ex.Message);
        }
    }
}
