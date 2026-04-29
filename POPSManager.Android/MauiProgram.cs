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

        // 🔹 Servicios del Core – interfaces y clases concretas

        // Paths y Logging
        builder.Services.AddSingleton<IPathsService, PathsServiceAndroid>();
        builder.Services.AddSingleton<ILoggingService, LoggingService>();
        builder.Services.AddSingleton<LoggingService>();

        // PathsService concreto por si alguna clase lo requiere directamente
        builder.Services.AddSingleton<PathsService>();

        // ConverterService sin delegados
        builder.Services.AddSingleton<ConverterService>(sp =>
            new ConverterService(null, null));

        // Notification, Settings, Localization
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<LocalizationService>();

        // Automation y sus dependencias
        builder.Services.AddSingleton<AutomationSettings>();
        builder.Services.AddSingleton<AutomationEngine>();

        // Cheats
        builder.Services.AddSingleton<CheatSettingsService>();
        builder.Services.AddSingleton<CheatManagerService>();

        // GameProcessor y sus posibles dependencias adicionales
        builder.Services.AddSingleton<MultiDiscManager>();
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