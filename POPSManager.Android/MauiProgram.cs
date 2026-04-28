using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
            .UseMauiApp<App>();

        // 🔹 Servicios
        builder.Services.AddSingleton<IPathsService, PathsService>();
        builder.Services.AddSingleton<ILoggingService, LoggingService>();
        builder.Services.AddSingleton<ConverterService>();
        builder.Services.AddSingleton<SettingsService>();

        // 🔹 ViewModels
        builder.Services.AddTransient<HomeViewModel>();

        // 🔹 Views
        builder.Services.AddTransient<HomePage>();

        return builder.Build();
    }
}