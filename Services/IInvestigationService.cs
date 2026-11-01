using System;
using System.Collections.Generic;
using MeridianClient.Models;

namespace MeridianClient.Services;

public interface IInvestigationService
{
    event Action? InvestigationsChanged;

    IReadOnlyList<InvestigationModel> GetAll();
    InvestigationModel? GetById(string id);
    void Save(InvestigationModel investigation);
    void Delete(string id);
    string ExportToFile(InvestigationModel investigation, string filePath);
    InvestigationModel ImportFromFile(string filePath);
}
