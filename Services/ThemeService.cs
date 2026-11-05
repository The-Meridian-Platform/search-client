using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace MeridianClient.Services;

public record ThemePalette(
    string Name,
    string DisplayNameRu,
    string DisplayNameEn,
    Color Background,
    Color CardBackground,
    Color CardBorder,
    Color SidebarBackground,
    Color Foreground,
    Color SecondaryForeground,
    Color Accent,
    bool IsDark
);

public interface IThemeService
{
    string CurrentPreset { get; }
    IReadOnlyList<ThemePalette> AvailablePresets { get; }
    void ApplyPreset(string? presetName);
    void ApplyCustomTheme(string? bgHex, string? cardHex, string? fgHex, string? accentHex);
    event Action? ThemeChanged;
}

public class ThemeService : IThemeService
{
    public static readonly ThemePalette CyberDark = new(
        "CyberDark", "Cyber Dark (По умолчанию)", "Cyber Dark (Default)",
        Color.FromRgb(11, 15, 23),            // #0B0F17 Deep Slate / Navy background
        Color.FromRgb(17, 24, 39),            // #111827 Dark Surface / Card
        Color.FromArgb(20, 255, 255, 255),    // #14FFFFFF Subtle Border (~8%)
        Color.FromRgb(14, 20, 32),            // #0E1420 Sidebar
        Color.FromRgb(248, 250, 252),         // #F8FAFC Clean Text Primary
        Color.FromRgb(148, 163, 184),         // #94A3B8 Slate-400 Text Secondary
        Color.FromRgb(6, 182, 212),           // #06B6D4 Modern Cyber Cyan Accent
        true
    );

    public static readonly ThemePalette OneDarkPro = new(
        "OneDarkPro", "One Dark Pro", "One Dark Pro",
        Color.FromRgb(40, 44, 52),           // #282C34 Distinct Gray-Blue One Dark
        Color.FromRgb(33, 37, 43),           // #21252B Deep Charcoal Card
        Color.FromArgb(35, 255, 255, 255),   // Crisp subtle border
        Color.FromRgb(30, 33, 39),           // #1E2127 Sidebar
        Color.FromRgb(220, 223, 228),        // #DCDFE4 Crisp Foreground
        Color.FromRgb(157, 165, 180),        // #9DA5B4 Slate-Purple Secondary
        Color.FromRgb(97, 175, 239),         // #61AFEF Vivid Atom Blue Accent
        true
    );

    public static readonly ThemePalette Nord = new(
        "Nord", "Nord Arctic", "Nord Arctic",
        Color.FromRgb(46, 52, 64),           // #2E3440 Polar Night Deep
        Color.FromRgb(59, 66, 82),           // #3B4252 Polar Night Card
        Color.FromArgb(50, 216, 222, 233),   // Frost border
        Color.FromRgb(38, 43, 53),           // Dark Frost Sidebar
        Color.FromRgb(236, 239, 244),        // #ECEFF4 Snow Storm
        Color.FromRgb(163, 172, 189),
        Color.FromRgb(136, 192, 208),        // #88C0D0 Frost Cyan Accent
        true
    );

    public static readonly ThemePalette CleanLight = new(
        "CleanLight", "Clean Light (Светлая)", "Clean Light (Light)",
        Color.FromRgb(248, 250, 252),        // #F8FAFC Soft Clean Light Background
        Color.FromRgb(255, 255, 255),        // #FFFFFF Pure White Card
        Color.FromArgb(30, 15, 23, 42),      // Contrast Border
        Color.FromRgb(241, 245, 249),        // Light Slate Sidebar
        Color.FromRgb(15, 23, 42),           // #0F172A Deep Ink Text
        Color.FromRgb(71, 85, 105),          // #475569 Slate Muted Text
        Color.FromRgb(14, 165, 233),         // #0EA5E9 Sky Blue Accent
        false
    );

    public static readonly IReadOnlyList<ThemePalette> Presets = new List<ThemePalette>
    {
        CyberDark,
        OneDarkPro,
        Nord,
        CleanLight
    };

    public IReadOnlyList<ThemePalette> AvailablePresets => Presets;

    public static ThemeService Instance { get; } = new();

    private string _currentPreset = "CyberDark";
    public string CurrentPreset => _currentPreset;

    public event Action? ThemeChanged;

    public void ApplyPreset(string? presetName)
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            presetName = "CyberDark";
        }
        else if (presetName.Equals("Dark", StringComparison.OrdinalIgnoreCase))
        {
            presetName = "CyberDark";
        }
        else if (presetName.Equals("Light", StringComparison.OrdinalIgnoreCase))
        {
            presetName = "CleanLight";
        }

        var preset = AvailablePresets.FirstOrDefault(p => p != null && p.Name.Equals(presetName, StringComparison.OrdinalIgnoreCase))
                     ?? CyberDark;

        _currentPreset = preset.Name;
        ApplyPalette(preset);
    }

    public void ApplyCustomTheme(string? bgHex, string? cardHex, string? fgHex, string? accentHex)
    {
        _currentPreset = "Custom";

        Color bg = ParseColorOrDefault(bgHex, CyberDark.Background);
        Color card = ParseColorOrDefault(cardHex, CyberDark.CardBackground);
        Color fg = ParseColorOrDefault(fgHex, CyberDark.Foreground);
        Color accent = ParseColorOrDefault(accentHex, CyberDark.Accent);

        // Calculate secondary & sidebar automatically
        Color sidebar = Color.FromArgb(
            bg.A,
            (byte)Math.Clamp(bg.R * 0.85, 0, 255),
            (byte)Math.Clamp(bg.G * 0.85, 0, 255),
            (byte)Math.Clamp(bg.B * 0.85, 0, 255)
        );

        Color cardBorder = Color.FromArgb(35, fg.R, fg.G, fg.B);
        Color secondary = Color.FromArgb(180, fg.R, fg.G, fg.B);

        bool isDark = (bg.R * 0.299 + bg.G * 0.587 + bg.B * 0.114) < 140;

        var customPalette = new ThemePalette(
            "Custom", "Кастомная тема", "Custom Theme",
            bg, card, cardBorder, sidebar, fg, secondary, accent, isDark
        );

        ApplyPalette(customPalette);
    }

    private void ApplyPalette(ThemePalette palette)
    {
        var appTheme = palette.IsDark ? ApplicationTheme.Dark : ApplicationTheme.Light;

        void ApplyToWpf()
        {
            if (Application.Current == null) return;

            ApplicationThemeManager.Apply(appTheme);

            var accentColor = palette.Accent;
            var subtleAccent = Color.FromArgb(28, accentColor.R, accentColor.G, accentColor.B);
            var borderAccent = Color.FromArgb(90, accentColor.R, accentColor.G, accentColor.B);

            void UpdateDict(ResourceDictionary dict)
            {
                // Force update color on existing brushes so elements already bound immediately repaint
                void SetOrUpdate(string key, Color color)
                {
                    if (dict[key] is SolidColorBrush brush && !brush.IsFrozen)
                    {
                        brush.Color = color;
                    }
                    else
                    {
                        dict[key] = new SolidColorBrush(color);
                    }
                }

                SetOrUpdate("AppBackgroundBrush", palette.Background);
                SetOrUpdate("CardBackgroundBrush", palette.CardBackground);
                SetOrUpdate("CardBorderBrush", palette.CardBorder);
                SetOrUpdate("SidebarBackgroundBrush", palette.SidebarBackground);
                SetOrUpdate("PrimaryTextBrush", palette.Foreground);
                SetOrUpdate("SecondaryTextBrush", palette.SecondaryForeground);
                SetOrUpdate("AccentBrush", palette.Accent);
                SetOrUpdate("SubtleAccentBrush", subtleAccent);
                SetOrUpdate("BorderAccentBrush", borderAccent);

                var edgeColor = palette.IsDark
                    ? Color.FromArgb(50, 255, 255, 255)
                    : Color.FromArgb(70, 71, 85, 105);
                SetOrUpdate("GraphEdgeBrush", edgeColor);

                var minimapBg = palette.IsDark
                    ? Color.FromArgb(208, 21, 24, 32)
                    : Color.FromArgb(235, 255, 255, 255);
                SetOrUpdate("MinimapBackgroundBrush", minimapBg);
            }

            UpdateDict(Application.Current.Resources);

            foreach (Window win in Application.Current.Windows)
            {
                UpdateDict(win.Resources);
            }
        }

        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(ApplyToWpf);
        }
        else
        {
            ApplyToWpf();
        }

        ThemeChanged?.Invoke();
    }

    private static Color ParseColorOrDefault(string? hex, Color fallback)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            if (!hex.StartsWith('#')) hex = "#" + hex;
            var obj = ColorConverter.ConvertFromString(hex);
            if (obj is Color c) return c;
        }
        catch
        {
            // fallback
        }
        return fallback;
    }
}
