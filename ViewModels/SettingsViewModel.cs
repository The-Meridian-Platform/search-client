using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeridianClient.Models;
using MeridianClient.Services;

namespace MeridianClient.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IMeridianApiClient _apiClient;

    [ObservableProperty]
    private string _backendUrl = "http://localhost:5168";

    [ObservableProperty]
    private bool _allowUnencryptedHttp2 = true;

    [ObservableProperty]
    private int _timeoutSeconds = 60;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private bool _isSuccessResult;

    [ObservableProperty]
    private string _savedStatus = string.Empty;

    public SettingsViewModel(ISettingsService settingsService, IMeridianApiClient apiClient)
    {
        _settingsService = settingsService;
        _apiClient = apiClient;

        BackendUrl = _settingsService.Settings.BackendUrl;
        AllowUnencryptedHttp2 = _settingsService.Settings.AllowUnencryptedHttp2;
        TimeoutSeconds = _settingsService.Settings.TimeoutSeconds;
    }

    [RelayCommand]
    public void SelectPresetHttps()
    {
        BackendUrl = "https://localhost:7013";
    }

    [RelayCommand]
    public void SelectPresetHttp()
    {
        BackendUrl = "http://localhost:5168";
        AllowUnencryptedHttp2 = true;
    }

    [RelayCommand]
    public async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestResult = "Проверка соединения с " + BackendUrl + "...";
        IsSuccessResult = false;

        try
        {
            var (success, message) = await _apiClient.TestConnectionAsync(BackendUrl);
            IsSuccessResult = success;
            TestResult = message;
        }
        catch (Exception ex)
        {
            IsSuccessResult = false;
            TestResult = $"Ошибка проверки: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    public void SaveSettings()
    {
        var settings = new AppSettings
        {
            BackendUrl = BackendUrl.Trim(),
            AllowUnencryptedHttp2 = AllowUnencryptedHttp2,
            TimeoutSeconds = TimeoutSeconds > 0 ? TimeoutSeconds : 60
        };

        _settingsService.SaveSettings(settings);
        SavedStatus = "Настройки успешно сохранены!";
    }
}
