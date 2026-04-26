using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

using POPSManager.Android.Services;
using POPSManager.Core.Services;
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
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Servicios del Core (ahora con registros reales)
        builder.Services.AddSingleton<ILoggingService, LoggingService>();
        builder.Services.AddSingleton<INotificationService, NotificationService>();
        builder.Services.AddSingleton<IPathsService, PathsServiceAndroid>(); // Android da la implementación real
        builder.Services.AddSingleton<ProgressService>();
        builder.Services.AddSingleton<ConverterService>();
        builder.Services.AddSingleton<GameProcessor>();

        // ViewModels
        builder.Services.AddSingleton<HomeViewModel>();

        // Vistas
        builder.Services.AddSingleton<HomePage>();

        return builder.Build();
    }
}