using System;
using POPSManager.Core.Models;

namespace POPSManager.Core.Services;

public class NotificationService : INotificationService
{
    public Action<string, NotificationType>? OnShowToast { get; set; }

    public void Show(string message, NotificationType type)
    {
        OnShowToast?.Invoke(message, type);
    }

    public void Success(string message) => Show(message, NotificationType.Success);
    public void Error(string message) => Show(message, NotificationType.Error);
    public void Warning(string message) => Show(message, NotificationType.Warning);
    public void Info(string message) => Show(message, NotificationType.Info);
}