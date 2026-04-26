using System;

namespace POPSManager.Logic
{
    public class Logger
    {
        private readonly Action<string> logAction;

        public Logger(Action<string> logAction)
        {
            this.logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
        }

        public void Info(string message)
        {
            logAction($"[INFO] [{DateTime.Now:HH:mm:ss}] {message}");
        }

        public void Warn(string message)
        {
            logAction($"[WARN] [{DateTime.Now:HH:mm:ss}] {message}");
        }

        public void Error(string message)
        {
            logAction($"[ERROR] [{DateTime.Now:HH:mm:ss}] {message}");
        }

        public void Log(string message)
        {
            Info(message);
        }
    }
}