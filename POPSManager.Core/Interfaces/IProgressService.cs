using System;

namespace POPSManager.Core.Services.Interfaces;

public interface IProgressService
{
    Action? OnStart { get; set; }
    Action? OnStop { get; set; }
    Action<int>? OnProgress { get; set; }
    Action<string>? OnStatus { get; set; }

    bool IsRunning { get; }

    void Start(string? status = null);
    void SetProgress(int value);
    void SetStatus(string text);
    void Stop();
    void Reset();
}