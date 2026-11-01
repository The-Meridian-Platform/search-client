using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MeridianClient.Views;

public partial class ColorPickerWindow : Window
{
    private static readonly string[] PopularColors =
    [
        "#08090E", "#10131F", "#1A1D2E", "#282C34", "#21252B", "#2E3440", "#3B4252", "#1E1E2E",
        "#F8FAFC", "#FFFFFF", "#E2E8F0", "#CBD5E1", "#94A3B8", "#64748B", "#475569", "#0F172A",
        "#00D2FF", "#38BDF8", "#61AFEF", "#88C0D0", "#00E599", "#50FA7B", "#A855F7", "#FF3366"
    ];

    private double _hue = 190;
    private double _saturation = 1.0;
    private double _value = 1.0;
    private bool _isUpdatingInternally;
    private bool _isDraggingSpectrum;

    public string SelectedHex { get; private set; } = "#00D2FF";

    public ColorPickerWindow(string initialHex)
    {
        InitializeComponent();
        BuildPopularSwatches();
        Loaded += (_, _) =>
        {
            SetCurrentColor(initialHex);
        };
    }

    private void BuildPopularSwatches()
    {
        swatchesPanel.Children.Clear();
        foreach (var hex in PopularColors)
        {
            var btn = new Button
            {
                Width = 24,
                Height = 24,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = hex
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(5),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255))
            };

            try
            {
                var col = (Color)ColorConverter.ConvertFromString(hex);
                border.Background = new SolidColorBrush(col);
            }
            catch
            {
                border.Background = Brushes.Gray;
            }

            btn.Content = border;
            btn.Click += (_, _) => SetCurrentColor(hex);
            swatchesPanel.Children.Add(btn);
        }
    }

    private void SetCurrentColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) hex = "#00D2FF";
        if (!hex.StartsWith('#')) hex = "#" + hex;

        try
        {
            var col = (Color)ColorConverter.ConvertFromString(hex);
            ColorToHsv(col, out _hue, out _saturation, out _value);

            _isUpdatingInternally = true;
            hueSlider.Value = _hue;
            UpdateHueBaseColor();
            UpdateSpectrumCursor();
            UpdateResultColor(col);
        }
        catch
        {
            // fallback
        }
        finally
        {
            _isUpdatingInternally = false;
        }
    }

    private void UpdateHueBaseColor()
    {
        var pureHue = HsvToColor(_hue, 1.0, 1.0);
        spectrumBaseRect.Fill = new SolidColorBrush(pureHue);
    }

    private void UpdateSpectrumCursor()
    {
        double width = spectrumCanvas.ActualWidth > 0 ? spectrumCanvas.ActualWidth : 340;
        double height = spectrumCanvas.ActualHeight > 0 ? spectrumCanvas.ActualHeight : 140;

        double x = _saturation * width;
        double y = (1.0 - _value) * height;

        Canvas.SetLeft(pickerCursor, Math.Clamp(x - 7, 0, width - 14));
        Canvas.SetTop(pickerCursor, Math.Clamp(y - 7, 0, height - 14));
    }

    private void UpdateResultColor(Color col)
    {
        var hex = $"#{col.R:X2}{col.G:X2}{col.B:X2}";
        SelectedHex = hex;
        txtHex.Text = hex;
        colorPreviewBorder.Background = new SolidColorBrush(col);
        txtRgbLabel.Text = $"R:{col.R} G:{col.G} B:{col.B}";
    }

    private void OnHueSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingInternally) return;

        _hue = hueSlider.Value;
        UpdateHueBaseColor();
        var col = HsvToColor(_hue, _saturation, _value);
        UpdateResultColor(col);
    }

    private void OnSpectrumMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSpectrum = true;
        spectrumCanvas.CaptureMouse();
        HandleSpectrumInput(e.GetPosition(spectrumCanvas));
    }

    private void OnSpectrumMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingSpectrum)
        {
            HandleSpectrumInput(e.GetPosition(spectrumCanvas));
        }
    }

    private void OnSpectrumMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSpectrum = false;
        spectrumCanvas.ReleaseMouseCapture();
    }

    private void HandleSpectrumInput(Point pt)
    {
        double width = spectrumCanvas.ActualWidth;
        double height = spectrumCanvas.ActualHeight;
        if (width <= 0 || height <= 0) return;

        double x = Math.Clamp(pt.X, 0, width);
        double y = Math.Clamp(pt.Y, 0, height);

        _saturation = x / width;
        _value = 1.0 - (y / height);

        UpdateSpectrumCursor();
        var col = HsvToColor(_hue, _saturation, _value);
        UpdateResultColor(col);
    }

    private void OnHexTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingInternally) return;

        var hex = txtHex.Text.Trim();
        if (hex.Length is 6 or 7)
        {
            if (!hex.StartsWith('#')) hex = "#" + hex;
            try
            {
                var col = (Color)ColorConverter.ConvertFromString(hex);
                ColorToHsv(col, out _hue, out _saturation, out _value);

                _isUpdatingInternally = true;
                hueSlider.Value = _hue;
                UpdateHueBaseColor();
                UpdateSpectrumCursor();
                SelectedHex = hex.ToUpperInvariant();
                colorPreviewBorder.Background = new SolidColorBrush(col);
                txtRgbLabel.Text = $"R:{col.R} G:{col.G} B:{col.B}";
                _isUpdatingInternally = false;
            }
            catch
            {
                // ignore syntax while typing
            }
        }
    }

    private void OnHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    #region HSV Math
    private static Color HsvToColor(double h, double s, double v)
    {
        h = (h % 360 + 360) % 360;
        s = Math.Clamp(s, 0, 1);
        v = Math.Clamp(v, 0, 1);

        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = v - c;

        double r = 0, g = 0, b = 0;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255)
        );
    }

    private static void ColorToHsv(Color color, out double h, out double s, out double v)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        v = max;
        s = max == 0 ? 0 : delta / max;

        if (delta == 0)
        {
            h = 0;
        }
        else if (max == r)
        {
            h = 60 * (((g - b) / delta) % 6);
        }
        else if (max == g)
        {
            h = 60 * (((b - r) / delta) + 2);
        }
        else
        {
            h = 60 * (((r - g) / delta) + 4);
        }

        if (h < 0) h += 360;
    }
    #endregion
}
