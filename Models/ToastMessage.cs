using System;
using Wpf.Ui.Controls;

namespace MeridianClient.Models;

public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}

public class ToastMessage
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; } = ToastType.Info;
    public int DurationSeconds { get; set; } = 4;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public string AccentColor => Type switch
    {
        ToastType.Success => "#00E599",
        ToastType.Warning => "#FFB800",
        ToastType.Error => "#FF3366",
        _ => "#00D2FF"
    };

    public string BadgeBackground => Type switch
    {
        ToastType.Success => "#2600E599",
        ToastType.Warning => "#26FFB800",
        ToastType.Error => "#26FF3366",
        _ => "#2600D2FF"
    };

    public string BadgeBorder => Type switch
    {
        ToastType.Success => "#5A00E599",
        ToastType.Warning => "#5AFFB800",
        ToastType.Error => "#5AFF3366",
        _ => "#5A00D2FF"
    };

    public SymbolRegular Symbol => Type switch
    {
        ToastType.Success => SymbolRegular.CheckmarkCircle24,
        ToastType.Warning => SymbolRegular.Warning24,
        ToastType.Error => SymbolRegular.DismissCircle24,
        _ => SymbolRegular.Info24
    };
}