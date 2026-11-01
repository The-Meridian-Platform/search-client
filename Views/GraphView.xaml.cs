using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using MeridianClient.Models;
using MeridianClient.ViewModels;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace MeridianClient.Views;

public partial class GraphView : UserControl
{
    private readonly Dictionary<GraphNode, FrameworkElement> _nodeVisuals = new();
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

            ((INotifyCollectionChanged)vm.Nodes).CollectionChanged -= OnNodesCollectionChanged;
            ((INotifyCollectionChanged)vm.Nodes).CollectionChanged += OnNodesCollectionChanged;

            ((INotifyCollectionChanged)vm.Edges).CollectionChanged -= OnEdgesCollectionChanged;
            ((INotifyCollectionChanged)vm.Edges).CollectionChanged += OnEdgesCollectionChanged;

            RebuildAllVisuals();

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
            foreach (GraphNode node in e.NewItems)
            {
                CreateVisualForNode(node);
            }
        }
    }

    private void OnEdgesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Edges are redrawn every frame in OnRenderFrame
    }

    private void RebuildAllVisuals()
    {
        _nodesContainer.Children.Clear();
        _nodeVisuals.Clear();

        if (DataContext is GraphViewModel vm)
        {
            foreach (var node in vm.Nodes)
            {
                CreateVisualForNode(node);
            }
        }
    }

    private const double NodeContainerWidth = 160.0;

    private void CreateVisualForNode(GraphNode node)
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

        if (node.Type == GraphNodeType.Target)
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
            FontSize = node.Radius * 0.95,
            Foreground = new SolidColorBrush(node.AccentColor),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        circle.Child = icon;
        container.Children.Add(circle);

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
            if (DataContext is GraphViewModel vm)
            {
                vm.SelectNode(node);
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

        _nodeVisuals[node] = container;
        _nodesContainer.Children.Add(container);

        Canvas.SetLeft(container, node.X - NodeContainerWidth / 2.0);
        Canvas.SetTop(container, node.Y - node.Radius);
    }

    private void OnRenderFrame(object? sender, EventArgs e)
    {
        if (DataContext is not GraphViewModel vm) return;

        // 1. Update node visual positions on canvas (perfect center tracking)
        foreach (var node in vm.Nodes)
        {
            if (_nodeVisuals.TryGetValue(node, out var visual))
            {
                Canvas.SetLeft(visual, node.X - NodeContainerWidth / 2.0);
                Canvas.SetTop(visual, node.Y - node.Radius);
                Canvas.SetTop(visual, node.Y - node.Radius);

                // Update highlight state
                if (visual is StackPanel panel && panel.Children[0] is Border border)
                {
                    if (node.IsSelected)
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
                    else if (node.Type != GraphNodeType.Target)
                    {
                        border.BorderThickness = new Thickness(1.5);
                        border.Effect = null;
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
        var edgePenActive = new Pen(brushAccent, 2.0);

        foreach (var edge in edges)
        {
            var line = new Line
            {
                X1 = edge.Source.X,
                Y1 = edge.Source.Y,
                X2 = edge.Target.X,
                Y2 = edge.Target.Y,
                Stroke = edge.IsHighlighted ? edgePenActive.Brush : _edgePenNormal.Brush,
                StrokeThickness = edge.IsHighlighted ? 2.0 : 1.2,
                Opacity = edge.IsHighlighted ? 0.95 : 0.45
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
}
