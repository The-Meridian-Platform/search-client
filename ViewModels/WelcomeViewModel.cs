using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeridianClient.Models;
using MeridianClient.Services;
using Microsoft.Win32;

namespace MeridianClient.ViewModels;

public partial class WelcomeViewModel : ObservableObject
{
    private readonly IInvestigationService _investigationService;
    private readonly ILogService _logService;
    private readonly ISettingsService _settingsService;
    private readonly IToastService _toastService;

    public event Action? NavigateToSearchRequested;
    public event Action<InvestigationModel>? OpenInvestigationRequested;
    public event Action? NavigateToGraphRequested;

    public ObservableCollection<InvestigationModel> RecentInvestigations { get; } = new();

    [ObservableProperty]
    private bool _hasRecentInvestigations;

    [ObservableProperty]
    private int _totalInvestigationsCount;

    [ObservableProperty]
    private bool _autoOpenLastSession;

    partial void OnAutoOpenLastSessionChanged(bool value)
    {
        _settingsService.Settings.ShowWelcomeScreenOnStartup = value;
        _settingsService.SaveSettings(_settingsService.Settings);
    }

    public WelcomeViewModel(
        IInvestigationService investigationService,
        ILogService logService,
        ISettingsService settingsService,
        IToastService toastService)
    {
        _investigationService = investigationService;
        _logService = logService;
        _settingsService = settingsService;
        _toastService = toastService;

        _autoOpenLastSession = _settingsService.Settings.ShowWelcomeScreenOnStartup;
        _investigationService.InvestigationsChanged += RefreshRecentInvestigations;
        RefreshRecentInvestigations();
    }

    public void RefreshRecentInvestigations()
    {
        RecentInvestigations.Clear();
        var all = _investigationService.GetAll();
        TotalInvestigationsCount = all.Count;

        foreach (var inv in all.Take(10))
        {
            RecentInvestigations.Add(inv);
        }

        HasRecentInvestigations = RecentInvestigations.Count > 0;
    }

    [RelayCommand]
    public void NewInvestigation()
    {
        NavigateToSearchRequested?.Invoke();
    }

    [RelayCommand]
    public void OpenInvestigation(InvestigationModel? model)
    {
        if (model == null) return;
        _settingsService.Settings.LastActiveInvestigationId = model.Id;
        _settingsService.SaveSettings(_settingsService.Settings);
        OpenInvestigationRequested?.Invoke(model);
    }

    [RelayCommand]
    public void OpenFromFile()
    {
        var ofd = new OpenFileDialog
        {
            Title = "Открыть файл расследования Meridian",
            Filter = "Файл расследования Meridian (*.json)|*.json|Все файлы (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                var imported = _investigationService.ImportFromFile(ofd.FileName);
                _settingsService.Settings.LastActiveInvestigationId = imported.Id;
                _settingsService.SaveSettings(_settingsService.Settings);
                _toastService.ShowSuccess("Расследование открыто", $"Загружен проект «{imported.Title}»");
                OpenInvestigationRequested?.Invoke(imported);
            }
            catch (Exception ex)
            {
                _logService.LogError("Welcome", $"Не удалось открыть файл расследования: {ex.Message}", ex);
                _toastService.ShowError("Ошибка открытия", ex.Message);
            }
        }
    }

    [RelayCommand]
    public void DeleteInvestigation(InvestigationModel? model)
    {
        if (model == null) return;
        _investigationService.Delete(model.Id);
        RefreshRecentInvestigations();
        _toastService.ShowInfo("Удалено", $"Расследование «{model.Title}» удалено");
    }

    [RelayCommand]
    public void GoToLiveGraph()
    {
        NavigateToGraphRequested?.Invoke();
    }
}
