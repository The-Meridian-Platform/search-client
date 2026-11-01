using MeridianClient.Models;

namespace MeridianClient.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    event Action? SettingsChanged;
    void SaveSettings(AppSettings newSettings);
}
