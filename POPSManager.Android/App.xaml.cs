namespace POPSManager.Android;

public partial class App : Application
{
    public App(NotificationService notifyService)
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
}