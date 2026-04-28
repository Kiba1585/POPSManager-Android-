using POPSManager.Core.Services;

namespace POPSManager.Android;

public partial class App : Application
{
    public App(NotificationService notifyService)
    {
        try
        {
            InitializeComponent();

            // Página de prueba ultra simple
            MainPage = new ContentPage
            {
                Content = new Label
                {
                    Text = "POPSManager funcionando",
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    FontSize = 24
                }
            };
        }
        catch (Exception ex)
        {
            // Si falla hasta esto, mostramos un error en un diálogo nativo
            MainPage = new ContentPage();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await MainPage.DisplayAlert("Error crítico", ex.ToString(), "OK");
            });
        }
    }
}