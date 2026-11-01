namespace MeridianClient.Models;

public class AppSettings
{
    public string BackendUrl { get; set; } = "http://localhost:5168";
    public bool AllowUnencryptedHttp2 { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 60;
    public string Language { get; set; } = "ru"; // "ru" or "en"
    public string ThemePreset { get; set; } = "CyberDark"; // "CyberDark", "OneDarkPro", "Nord", "CleanLight", "Custom"
    public string CustomBackgroundColor { get; set; } = "#0D1117";
    public string CustomCardColor { get; set; } = "#161B22";
    public string CustomForegroundColor { get; set; } = "#F0F6FC";
    public string CustomAccentColor { get; set; } = "#00D2FF";
    public bool EnableToasts { get; set; } = true;
}
