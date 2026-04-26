using POPSManager.Core.Models;

namespace POPSManager.Core.Services;

public interface INotificationService
{
    void Show(string message, NotificationType type);
    void Success(string message);
    void Error(string message);
    void Warning(string message);
    void Info(string message);
}