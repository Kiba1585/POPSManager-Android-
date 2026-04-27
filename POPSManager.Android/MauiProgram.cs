using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using POPSManager.Android.Services;
using POPSManager.Core.Services;
using POPSManager.Core.Services.Interfaces;   // <-- añadido para INotificationService
using POPSManager.Core.Localization;
using POPSManager.Core.Logic.Automation;
using POPSManager.Core.Settings;
using POPSManager.Core.Logic.Cheats;
using POPSManager.Android.Views;
using POPSManager.Android.ViewModels;

namespace POPSManager.Android;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // --- Servicios del Core ---
        builder.Services.AddSingleton<ILoggingService, LoggingService>();
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<NotificationService>());
        builder.Services.AddSingleton<IPathsService, PathsServiceAndroid>();
        builder.Services.AddSingleton<ConverterService>();

        // --- Settings y Localización ---
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<LocalizationService>();

        // --- GameProcessor con todas sus dependencias ---
        builder.Services.AddSingleton(sp =>
        {
            var log = sp.GetRequiredService<LoggingService>();
            var notify = sp.GetRequiredService<NotificationService>();
            var paths = sp.GetRequiredService<IPathsService>() as PathsService ?? new PathsService();
            var settings = sp.GetRequiredService<SettingsService>();
            var loc = sp.GetRequiredService<LocalizationService>();
            var auto = new AutomationEngine(settings, notify, log);
            var cheatSvc = new CheatSettingsService(paths.RootFolder, log.Info);
            var cheatMgr = new CheatManagerService(cheatSvc, log.Info);

            return new GameProcessor(log, notify, paths, cheatSvc, cheatMgr, settings, auto, loc);
        });

        // --- ViewModels ---
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<ConvertViewModel>();
        builder.Services.AddSingleton<ProcessPopsViewModel>();

        // --- Vistas ---
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<ConvertPage>();
        builder.Services.AddSingleton<ProcessPopsPage>();

        // --- Shell ---
        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}