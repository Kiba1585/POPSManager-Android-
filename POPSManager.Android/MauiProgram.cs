using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using POPSManager.Android.Services;
using POPSManager.Core.Services;
using POPSManager.Core.Localization;
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
            .UseMauiCommunityToolkit()            // ← Necesario para FolderPicker
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
        builder.Services.AddSingleton<ProgressService>();
        builder.Services.AddSingleton<ConverterService>();
        builder.Services.AddSingleton<GameProcessor>();

        // --- Settings y Localización ---
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<LocalizationService>();

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