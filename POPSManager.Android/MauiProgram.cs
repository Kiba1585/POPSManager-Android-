using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;   // para FileSystem
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Maui;
using POPSManager.Core.Services;
using POPSManager.Core.Logic.Automation;
using POPSManager.Core.Logic.Cheats;
using POPSManager.Core.Localization;
using POPSManager.Core.Settings;
using POPSManager.Android.ViewModels;
using POPSManager.Android.Views;
using POPSManager.Android.Services;

namespace POPSManager.Android;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        // 🔹 Servicios del Core
        builder.Services.AddSingleton<IPathsService, PathsServiceAndroid>();
        builder.Services.AddSingleton<ILoggingService, LoggingService>();
        builder.Services.AddSingleton<LoggingService>();

        // SettingsService con carpeta real de la app
        builder.Services.AddSingleton<SettingsService>(sp =>
            new SettingsService(FileSystem.AppDataDirectory));

        // ConverterService
        builder.Services.AddSingleton<ConverterService>(sp =>
            new ConverterService(null, null));

        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<LocalizationService>();

        // Automation
        builder.Services.AddSingleton<AutomationSettings>();
        builder.Services.AddSingleton<AutomationEngine>();

        // Cheats
        builder.Services.AddSingleton<CheatSettingsService>(sp =>
        {
            var paths = sp.GetRequiredService<IPathsService>();
            var log = sp.GetRequiredService<ILoggingService>();
            return new CheatSettingsService(paths.RootFolder, msg => log.Log(msg));
        });
        builder.Services.AddSingleton<CheatManagerService>();

        builder.Services.AddSingleton<GameProcessor>();

        // 🔹 ViewModels
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<ConvertViewModel>();
        builder.Services.AddTransient<ProcessPopsViewModel>();

        // 🔹 Páginas
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ConvertPage>();
        builder.Services.AddTransient<ProcessPopsPage>();

        // 🔹 AppShell
        builder.Services.AddTransient<AppShell>();

        return builder.Build();
    }
}