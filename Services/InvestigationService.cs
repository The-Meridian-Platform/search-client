using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MeridianClient.Models;

namespace MeridianClient.Services;

public class InvestigationService : IInvestigationService
{
    private static readonly string StorageFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MeridianClient",
        "investigations.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly List<InvestigationModel> _investigations = new();
    private readonly ILogService _logService;

    public event Action? InvestigationsChanged;

    public InvestigationService(ILogService logService)
    {
        _logService = logService;
        LoadFromDisk();
    }

    public IReadOnlyList<InvestigationModel> GetAll()
    {
        lock (_investigations)
        {
            return _investigations.OrderByDescending(i => i.UpdatedAt).ToList();
        }
    }

    public InvestigationModel? GetById(string id)
    {
        lock (_investigations)
        {
            return _investigations.FirstOrDefault(i => i.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void Save(InvestigationModel investigation)
    {
        if (investigation == null) return;

        investigation.UpdatedAt = DateTime.UtcNow;

        lock (_investigations)
        {
            int index = _investigations.FindIndex(i => i.Id.Equals(investigation.Id, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                _investigations[index] = investigation;
            }
            else
            {
                _investigations.Add(investigation);
            }

            PersistToDisk();
        }

        InvestigationsChanged?.Invoke();
    }

    public void Delete(string id)
    {
        lock (_investigations)
        {
            int removed = _investigations.RemoveAll(i => i.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                PersistToDisk();
            }
        }

        InvestigationsChanged?.Invoke();
    }

    public string ExportToFile(InvestigationModel investigation, string filePath)
    {
        var json = JsonSerializer.Serialize(investigation, JsonOptions);
        File.WriteAllText(filePath, json);
        _logService.LogInfo("Investigation", $"Расследование [{investigation.Title}] экспортировано в {filePath}");
        return filePath;
    }

    public InvestigationModel ImportFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Файл расследования не найден: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        var model = JsonSerializer.Deserialize<InvestigationModel>(json, JsonOptions);
        if (model == null || string.IsNullOrWhiteSpace(model.Id))
        {
            throw new InvalidOperationException("Некорректный формат файла расследования.");
        }

        // Save imported model into workspace
        Save(model);
        _logService.LogInfo("Investigation", $"Расследование [{model.Title}] успешно импортировано ({model.SessionIds.Count} сессий, {model.CachedFindings.Count} находок)");
        return model;
    }

    private void LoadFromDisk()
    {
        try
        {
            if (File.Exists(StorageFilePath))
            {
                var json = File.ReadAllText(StorageFilePath);
                var items = JsonSerializer.Deserialize<List<InvestigationModel>>(json, JsonOptions);
                if (items != null)
                {
                    _investigations.Clear();
                    _investigations.AddRange(items);
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("Investigation", $"Не удалось загрузить расследования из {StorageFilePath}: {ex.Message}", ex);
        }
    }

    private void PersistToDisk()
    {
        try
        {
            var dir = Path.GetDirectoryName(StorageFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(_investigations, JsonOptions);
            File.WriteAllText(StorageFilePath, json);
        }
        catch (Exception ex)
        {
            _logService.LogError("Investigation", $"Не удалось сохранить расследования в {StorageFilePath}: {ex.Message}", ex);
        }
    }
}
