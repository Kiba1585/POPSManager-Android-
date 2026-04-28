using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Maui;
using POPSManager.Core.Services;
using POPSManager.Android.ViewModels;
using POPSManager.Android.Views;

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
        builder.Services.AddSingleton<IPathsService, PathsService>();
        builder.Services.AddSingleton<ILoggingService, LoggingService>();
        builder.Services.AddSingleton<ConverterService>();
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<NotificationService>();

        // 🔹 ViewModels
        builder.Services.AddTransient<HomeViewModel>();

        // 🔹 Páginas (solo la implementada)
        builder.Services.AddTransient<HomePage>();

        // 🔹 AppShell
        builder.Services.AddTransient<AppShell>();

        return builder.Build();
    }
}