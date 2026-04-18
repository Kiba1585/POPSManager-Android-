using POPSManager.Core.Services;

namespace POPSManager.Android.Services;

public class LoggingServiceAndroid : ILoggingService
{
    public void Log(string message)
    {
        System.Diagnostics.Debug.WriteLine(message);
    }
}