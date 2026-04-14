using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using POPSManager.Core.Services;

namespace POPSManager.Android;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>();

        // Servicios Android
        builder.Services.AddSingleton<IPathsService, PathsServiceAndroid>();
        builder.Services.AddSingleton<ILoggingService, LoggingServiceAndroid>();
        builder.Services.AddSingleton<INotificationService, NotificationServiceAndroid>();

        // Core
        builder.Services.AddSingleton<GameProcessor>();
        builder.Services.AddSingleton<ConverterService>();

        // ViewModels
        builder.Services.AddSingleton<HomeViewModel>();

        // Views
        builder.Services.AddSingleton<HomePage>();

        return builder.Build();
    }
}
