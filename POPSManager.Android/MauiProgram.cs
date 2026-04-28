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

        // 🔹 Servicios del Core (interfaces + implementaciones)
        builder.Services.AddSingleton<IPathsService, PathsService>();
        builder.Services.AddSingleton<ILoggingService, LoggingService>();
        builder.Services.AddSingleton<ConverterService>();
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<NotificationService>();
        // LocalizationService no está disponible en Core; lo omitimos.

        // 🔹 ViewModels
        builder.Services.AddTransient<HomeViewModel>();

        // 🔹 Páginas (Views)
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ConvertPage>();
        builder.Services.AddTransient<ProcessPopsPage>();

        // 🔹 AppShell
        builder.Services.AddTransient<AppShell>();

        return builder.Build();
    }
}