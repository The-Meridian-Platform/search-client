using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Appearance;
using MeridianClient.Services;
using MeridianClient.ViewModels;

namespace MeridianClient;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), args.ExceptionObject.ToString());
            };

            DispatcherUnhandledException += (s, args) =>
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), args.Exception.ToString());
            };

            // Ensure HTTP/2 unencrypted support is enabled globally
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Load saved settings & apply theme + language
            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
            var themeService = _serviceProvider.GetRequiredService<IThemeService>();
            var locService = _serviceProvider.GetRequiredService<ILocalizationService>();

            if (settingsService.Settings.ThemePreset == "Custom")
            {
                themeService.ApplyCustomTheme(
                    settingsService.Settings.CustomBackgroundColor,
                    settingsService.Settings.CustomCardColor,
                    settingsService.Settings.CustomForegroundColor,
                    settingsService.Settings.CustomAccentColor);
            }
            else
            {
                themeService.ApplyPreset(settingsService.Settings.ThemePreset);
            }
            locService.SetLanguage(settingsService.Settings.Language);

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
            mainWindow.Activate();
            mainWindow.Focus();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.txt"), ex.ToString());
            throw;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILocalizationService>(LocalizationService.Instance);
        services.AddSingleton<IThemeService>(ThemeService.Instance);
        services.AddSingleton<IHealthCheckService, HealthCheckService>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<IInvestigationService, InvestigationService>();
        services.AddSingleton<IMeridianApiClient, MeridianApiClient>();
        services.AddSingleton<IMeridianStreamClient, MeridianStreamClient>();

        // ViewModels
        services.AddSingleton<WelcomeViewModel>();
        services.AddSingleton<SearchViewModel>();
        services.AddSingleton<GraphViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<LogsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
