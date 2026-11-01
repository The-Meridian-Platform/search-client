using System;
using MeridianClient.Models;
using MeridianClient.Services;
using MeridianClient.ViewModels;
using Xunit;

namespace MeridianClient.Tests;

public class LogsViewModelTests
{
    [Fact]
    public void LogService_RecordsAndTracksCounts()
    {
        var logService = new LogService();
        Assert.Equal(0, logService.TotalCount);

        logService.LogInfo("REST", "Test info message");
        logService.LogWarning("HEALTH", "Test warning");
        logService.LogError("gRPC", "Test error", new InvalidOperationException("Fail"));

        Assert.Equal(3, logService.TotalCount);
        Assert.Equal(1, logService.InfoCount);
        Assert.Equal(1, logService.WarningCount);
        Assert.Equal(1, logService.ErrorCount);

        var all = logService.GetAllLogs();
        Assert.Equal(3, all.Count);
        Assert.Contains("Test info message", all[0].Message);
        Assert.Contains("Fail", all[2].Details);

        logService.Clear();
        Assert.Equal(0, logService.TotalCount);
        Assert.Equal(0, logService.InfoCount);
        Assert.Equal(0, logService.WarningCount);
        Assert.Equal(0, logService.ErrorCount);
        Assert.Empty(logService.GetAllLogs());
    }

    [Fact]
    public void LogsViewModel_FiltersByLevelAndQuery()
    {
        var logService = new LogService();
        var vm = new LogsViewModel(logService);

        logService.LogInfo("REST", "Querying endpoint /api/search-sessions");
        logService.LogWarning("HEALTH", "High ping 250ms");
        logService.LogError("gRPC", "Connection reset by peer");

        vm.RefreshFilteredList();

        // Level filtering
        vm.SelectedLevel = "Error";
        Assert.All(vm.FilteredLogs, l => Assert.Equal(ClientLogLevel.Error, l.Level));

        // Text query filtering
        vm.SelectedLevel = "All";
        vm.FilterText = "search-sessions";
        Assert.Single(vm.FilteredLogs);
        Assert.Contains("search-sessions", vm.FilteredLogs[0].Message);

        // Clear query
        vm.FilterText = string.Empty;
        Assert.Equal(vm.TotalCount, vm.FilteredLogs.Count);
    }
}
