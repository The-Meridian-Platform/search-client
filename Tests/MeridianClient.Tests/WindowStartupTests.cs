using System.Threading;
using MeridianClient.ViewModels;

namespace MeridianClient.Tests;

public class WindowStartupTests
{
    [Fact]
    public void MainWindow_CanBeCreatedAndShownOnSTAThread()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = new System.Windows.Application();
                var logService = new Services.LogService();
                var settingsService = new Services.SettingsService();
                var healthService = new Services.HealthCheckService(settingsService, logService);
                var searchVm = new SearchViewModel(new SearchViewModelTests.DummyApiClient(), new SearchViewModelTests.DummyStreamClient(), logService);
                var histVm = new HistoryViewModel(new SearchViewModelTests.DummyApiClient());
                var logsVm = new LogsViewModel(logService);
                var setVm = new SettingsViewModel(settingsService, new SearchViewModelTests.DummyApiClient());
                var mainVm = new MainViewModel(searchVm, histVm, logsVm, setVm, healthService);

                var win = new MainWindow(mainVm);
                win.Show();
                Assert.True(win.IsVisible);
                win.Close();
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(5000);

        if (threadEx != null)
        {
            throw new Exception("STA Window failure: " + threadEx.ToString(), threadEx);
        }
    }
}
