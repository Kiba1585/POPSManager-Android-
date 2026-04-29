using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Maui;
using POPSManager.Core.Services;
using POPSManager.Core.Logic.Automation;
using POPSManager.Core.Logic.Cheats;
using POPSManager.Core.Localization;
using POPSManager.Core.Settings;
using POPSManager.Android.ViewModels;
using POPSManager.Android.Views;
using POPSManager.Android.Services;   // Para PathsServiceAndroid

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

        // Interfaces → implementaciones Android
        builder.Services.AddSingleton<IPathsService, PathsServiceAndroid>();   // Usar versión Android
        builder.Services.AddSingleton<ILoggingService, LoggingService>();

        // Clases concretas que esperan algunos constructores (como GameProcessor)
        builder.Services.AddSingleton<LoggingService>();          // Resuelve LoggingService directamente
        builder.Services.AddSingleton<PathsService>();           // Resuelve PathsService directamente

        builder.Services.AddSingleton<ConverterService>();
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<LocalizationService>();
        builder.Services.AddSingleton<CheatSettingsService>();
        builder.Services.AddSingleton<CheatManagerService>();
        builder.Services.AddSingleton<AutomationEngine>();
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