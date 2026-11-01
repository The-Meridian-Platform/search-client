using MeridianClient.Services;

namespace MeridianClient.Models;

public class ParsedDisposableEmailData
{
    public string Email { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public bool IsDisposable { get; set; }
    public string Category { get; set; } = "Burner / Temporary Mail";

    public string StatusBadgeText => IsDisposable ? "BURNER / DISPOSABLE" : "LEGITIMATE MAIL";
    public string StatusBadgeColor => IsDisposable ? "#FF3366" : "#00E599";
    public string StatusBadgeBackground => IsDisposable ? "#25FF3366" : "#2000E599";
    public string StatusIconSymbol => IsDisposable ? "ShieldError24" : "ShieldCheckmark24";
    public string StatusWarningTitle => IsDisposable
        ? LocalizationService.Instance.GetString("Search_Card_Disposable_BurnerTitle")
        : LocalizationService.Instance.GetString("Search_Card_Disposable_LegitTitle");
    public string StatusWarningMessage => IsDisposable
        ? LocalizationService.Instance.GetString("Search_Card_Disposable_BurnerMsg")
        : LocalizationService.Instance.GetString("Search_Card_Disposable_LegitMsg");
}
