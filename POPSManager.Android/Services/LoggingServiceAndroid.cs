using POPSManager.Core.Services;

namespace POPSManager.Android;

public class LoggingServiceAndroid : ILoggingService
{
    public void Log(string message)
    {
        System.Diagnostics.Debug.WriteLine(message);
    }
}
