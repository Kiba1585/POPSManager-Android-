using POPSManager.Core.Services;

namespace POPSManager.Android;

public partial class App : Application
{
    public App(NotificationService notifyService)
    {
        try
        {
            InitializeComponent();
            MainPage = new AppShell();

            notifyService.OnShowToast = (msg, type) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage?.DisplayAlert("POPSManager", msg, "OK");
                });
            };
        }
        catch (Exception ex)
        {
            // Si algo falla al crear AppShell o configurar el toast, mostramos el error directamente
            MainPage = new ContentPage
            {
                Content = new Label
                {
                    Text = $"Error al iniciar la aplicación: {ex.Message}",
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                }
            };
        }
    }
}