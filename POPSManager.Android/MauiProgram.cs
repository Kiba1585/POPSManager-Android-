using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

// Android services
using POPSManager.Android.Services;

// Core services
using POPSManager.Core.Services;
using POPSManager.Core;

// Views & ViewModels
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

        // Android-specific services
        builder.Services.AddSingleton<IPathsService, PathsServiceAndroid>();
        builder.Services.AddSingleton<ILoggingService, LoggingServiceAndroid>();
        builder.Services.AddSingleton<INotificationService, NotificationServiceAndroid>();

        // Core services
        builder.Services.AddSingleton<GameProcessor>();
        builder.Services.AddSingleton<ConverterService>();

        // ViewModels
        builder.Services.AddSingleton<HomeViewModel>();

        // Views
        builder.Services.AddSingleton<HomePage>();

        return builder.Build();
    }
}
