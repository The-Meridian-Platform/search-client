using System;
using MeridianClient.Models;

namespace MeridianClient.Services;

public interface IToastService
{
    event Action<ToastMessage>? ToastRequested;
    event Action? DismissRequested;

    void Show(string title, string message, ToastType type = ToastType.Info, int durationSeconds = 4);
    void ShowSuccess(string title, string message, int durationSeconds = 4);
    void ShowError(string title, string message, int durationSeconds = 5);
    void ShowWarning(string title, string message, int durationSeconds = 4);
    void ShowInfo(string title, string message, int durationSeconds = 4);
    void Dismiss();
}