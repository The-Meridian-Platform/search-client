using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeridianClient.Models;
using MeridianClient.Services;

namespace MeridianClient.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IMeridianApiClient _apiClient;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _locService;
    private readonly IToastService _toastService;

    [ObservableProperty]
    private string _backendUrl = "http://localhost:5168";

    [ObservableProperty]
    private bool _allowUnencryptedHttp2 = true;

    [ObservableProperty]
    private int _timeoutSeconds = 60;

    [ObservableProperty]
    private string _selectedThemePreset = "CyberDark";

    [ObservableProperty]
    private string _customBgColor = "#0D1117";

    [ObservableProperty]
    private string _customCardColor = "#161B22";

    [ObservableProperty]
    private string _customFgColor = "#F0F6FC";

    [ObservableProperty]
    private string _customAccentColor = "#00D2FF";

    [ObservableProperty]
    private string _selectedLanguage = "ru";

    [ObservableProperty]
    private bool _enableToasts = true;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private bool _isSuccessResult;

    public class ThemeOption
    {
        public string Key { get; }
        public string Title { get; }

        public ThemeOption(string key, string title)
        {
            Key = key;
            Title = title;
        }

        public override string ToString() => Title;
    }

    public ObservableCollection<ThemeOption> AvailableThemeOptions { get; } = new()
    {
        new("CyberDark", "Cyber Dark (По умолчанию)"),
        new("OneDarkPro", "One Dark Pro"),
        new("Nord", "Nord Arctic"),
        new("CleanLight", "Clean Light (Светлая)"),
        new("Custom", "Кастомная тема")
    };

    private ThemeOption? _selectedThemeOption;
    public ThemeOption? SelectedThemeOption
    {
        get => _selectedThemeOption;
        set
        {
            if (SetProperty(ref _selectedThemeOption, value) && value != null)
            {
                SelectedThemePreset = value.Key;
            }
        }
    }

    public SettingsViewModel(
        ISettingsService settingsService,
        IMeridianApiClient apiClient,
        IThemeService themeService,
        ILocalizationService locService,
        IToastService? toastService = null)
    {
        _settingsService = settingsService;
        _apiClient = apiClient;
        _themeService = themeService;
        _locService = locService;
        _toastService = toastService ?? new ToastService(settingsService);

        var s = _settingsService.Settings;
        BackendUrl = s.BackendUrl;
        AllowUnencryptedHttp2 = s.AllowUnencryptedHttp2;
        TimeoutSeconds = s.TimeoutSeconds;
        SelectedThemePreset = string.IsNullOrWhiteSpace(s.ThemePreset) ? "CyberDark" : s.ThemePreset;
        SelectedThemeOption = AvailableThemeOptions.FirstOrDefault(o => o.Key.Equals(SelectedThemePreset, StringComparison.OrdinalIgnoreCase))
                              ?? AvailableThemeOptions[0];
        CustomBgColor = string.IsNullOrWhiteSpace(s.CustomBackgroundColor) ? "#0D1117" : s.CustomBackgroundColor;
        CustomCardColor = string.IsNullOrWhiteSpace(s.CustomCardColor) ? "#161B22" : s.CustomCardColor;
        CustomFgColor = string.IsNullOrWhiteSpace(s.CustomForegroundColor) ? "#F0F6FC" : s.CustomForegroundColor;
        CustomAccentColor = string.IsNullOrWhiteSpace(s.CustomAccentColor) ? "#00D2FF" : s.CustomAccentColor;
        SelectedLanguage = string.IsNullOrWhiteSpace(s.Language) ? "ru" : s.Language;
        EnableToasts = s.EnableToasts;
    }

    public bool IsCustomThemeSelected => SelectedThemePreset == "Custom";

    partial void OnEnableToastsChanged(bool value) => AutoSave();
    partial void OnBackendUrlChanged(string value) => AutoSave();
    partial void OnAllowUnencryptedHttp2Changed(bool value) => AutoSave();
    partial void OnTimeoutSecondsChanged(int value) => AutoSave();

    partial void OnSelectedThemePresetChanged(string value)
    {
        OnPropertyChanged(nameof(IsCustomThemeSelected));
        if (SelectedThemeOption?.Key != value)
        {
            _selectedThemeOption = AvailableThemeOptions.FirstOrDefault(o => o.Key.Equals(value, StringComparison.OrdinalIgnoreCase));
            OnPropertyChanged(nameof(SelectedThemeOption));
        }

        if (value == "Custom")
        {
            _themeService.ApplyCustomTheme(CustomBgColor, CustomCardColor, CustomFgColor, CustomAccentColor);
        }
        else
        {
            _themeService.ApplyPreset(value);
        }
        AutoSave();
    }

    partial void OnCustomBgColorChanged(string value) => ApplyCustomAndSave();
    partial void OnCustomCardColorChanged(string value) => ApplyCustomAndSave();
    partial void OnCustomFgColorChanged(string value) => ApplyCustomAndSave();
    partial void OnCustomAccentColorChanged(string value) => ApplyCustomAndSave();

    private void ApplyCustomAndSave()
    {
        if (SelectedThemePreset == "Custom")
        {
            _themeService.ApplyCustomTheme(CustomBgColor, CustomCardColor, CustomFgColor, CustomAccentColor);
        }
        AutoSave();
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        _locService.SetLanguage(value);
        AutoSave();
    }

    [RelayCommand]
    public void PickCustomBgColor()
    {
        var picked = ShowColorPickerDialog(CustomBgColor);
        if (!string.IsNullOrEmpty(picked))
        {
            CustomBgColor = picked;
        }
    }

    [RelayCommand]
    public void PickCustomCardColor()
    {
        var picked = ShowColorPickerDialog(CustomCardColor);
        if (!string.IsNullOrEmpty(picked))
        {
            CustomCardColor = picked;
        }
    }

    [RelayCommand]
    public void PickCustomFgColor()
    {
        var picked = ShowColorPickerDialog(CustomFgColor);
        if (!string.IsNullOrEmpty(picked))
        {
            CustomFgColor = picked;
        }
    }

    [RelayCommand]
    public void PickCustomAccentColor()
    {
        var picked = ShowColorPickerDialog(CustomAccentColor);
        if (!string.IsNullOrEmpty(picked))
        {
            CustomAccentColor = picked;
        }
    }

    private static string? ShowColorPickerDialog(string initialHex)
    {
        var picker = new Views.ColorPickerWindow(initialHex)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (picker.ShowDialog() == true)
        {
            return picker.SelectedHex;
        }

        return null;
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
        TestResult = (_locService.CurrentLanguage == "en" ? "Testing connection to " : "Проверка соединения с ") + BackendUrl + "...";
        IsSuccessResult = false;

        try
        {
            var (success, message) = await _apiClient.TestConnectionAsync(BackendUrl);
            IsSuccessResult = success;
            TestResult = message;
            if (success)
            {
                _toastService.ShowSuccess("Шлюз доступен", message);
            }
            else
            {
                _toastService.ShowError("Шлюз недоступен", message);
            }
        }
        catch (Exception ex)
        {
            IsSuccessResult = false;
            TestResult = (_locService.CurrentLanguage == "en" ? "Error: " : "Ошибка проверки: ") + ex.Message;
            _toastService.ShowError("Ошибка подключения", ex.Message);
        }
        finally
        {
            IsTesting = false;
        }
    }

    private void AutoSave()
    {
        var settings = new AppSettings
        {
            BackendUrl = BackendUrl.Trim(),
            AllowUnencryptedHttp2 = AllowUnencryptedHttp2,
            TimeoutSeconds = TimeoutSeconds > 0 ? TimeoutSeconds : 60,
            Language = SelectedLanguage,
            ThemePreset = SelectedThemePreset,
            CustomBackgroundColor = CustomBgColor,
            CustomCardColor = CustomCardColor,
            CustomForegroundColor = CustomFgColor,
            CustomAccentColor = CustomAccentColor,
            EnableToasts = EnableToasts
        };

        _settingsService.SaveSettings(settings);
    }
}
