using POPSManager.Core.Services;

namespace POPSManager.Android.Services;

public class NotificationServiceAndroid : INotificationService
{
    public void Show(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current?.MainPage?.DisplayAlert("POPSManager", message, "OK");
        });
    }
}