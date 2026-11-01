using System.IO;
using System.Text.Json;
using MeridianClient.Models;

namespace MeridianClient.Services;

public class SettingsService : ISettingsService
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MeridianClient",
        "settings.json");

    private AppSettings _settings;

    public AppSettings Settings => _settings;

    public event Action? SettingsChanged;

    public SettingsService()
    {
        _settings = LoadSettings();
        ApplyRuntimeSwitches(_settings);
    }

    public void SaveSettings(AppSettings newSettings)
    {
        _settings = newSettings ?? new AppSettings();
        ApplyRuntimeSwitches(_settings);

        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Ignore persistence errors in restricted environments
        }

        SettingsChanged?.Invoke();
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Fallback to default
        }

        return new AppSettings();
    }

    private void ApplyRuntimeSwitches(AppSettings settings)
    {
        if (settings.AllowUnencryptedHttp2)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }
    }
}
