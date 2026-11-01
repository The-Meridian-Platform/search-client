using System;
using System.Windows;
using MeridianClient.Models;

namespace MeridianClient.Services;

public class ToastService : IToastService
{
    private readonly ISettingsService _settingsService;

    public event Action<ToastMessage>? ToastRequested;
    public event Action? DismissRequested;

    public ToastService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Show(string title, string message, ToastType type = ToastType.Info, int durationSeconds = 4)
    {
        if (!_settingsService.Settings.EnableToasts)
        {
            return;
        }

        var toast = new ToastMessage
        {
            Title = title,
            Message = message,
            Type = type,
            DurationSeconds = Math.Max(2, durationSeconds)
        };

        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.InvokeAsync(() => ToastRequested?.Invoke(toast));
        }
        else
        {
            ToastRequested?.Invoke(toast);
        }
    }

    public void ShowSuccess(string title, string message, int durationSeconds = 4)
        => Show(title, message, ToastType.Success, durationSeconds);

    public void ShowError(string title, string message, int durationSeconds = 5)
        => Show(title, message, ToastType.Error, durationSeconds);

    public void ShowWarning(string title, string message, int durationSeconds = 4)
        => Show(title, message, ToastType.Warning, durationSeconds);

    public void ShowInfo(string title, string message, int durationSeconds = 4)
        => Show(title, message, ToastType.Info, durationSeconds);

    public void Dismiss()
    {
        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.InvokeAsync(() => DismissRequested?.Invoke());
        }
        else
        {
            DismissRequested?.Invoke();
        }
    }
}