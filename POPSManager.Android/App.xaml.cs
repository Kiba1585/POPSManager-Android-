public partial class App : Application
{
    public App(NotificationService notifyService, POPSManager.Android.Views.HomePage home)
    {
        InitializeComponent();
        MainPage = new NavigationPage(home);

        notifyService.OnShowToast = (msg, type) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage?.DisplayAlert("POPSManager", msg, "OK");
            });
        };
    }
}