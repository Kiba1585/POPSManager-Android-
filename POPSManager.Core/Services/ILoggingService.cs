namespace POPSManager.Core.Services;

public interface ILoggingService
{
    void Log(string message);
    void Info(string msg);
    void Warn(string msg);
    void Error(string msg);
}