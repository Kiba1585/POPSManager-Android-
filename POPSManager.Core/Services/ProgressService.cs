using System;
using System.Diagnostics;

namespace POPSManager.Core.Services;

public class ProgressService
{
    public Action? OnStart { get; set; }
    public Action? OnStop { get; set; }
    public Action<int>? OnProgress { get; set; }
    public Action<string>? OnStatus { get; set; }

    public bool IsRunning { get; private set; }

    private readonly Stopwatch _throttleWatch = new();
    private int _lastReportedValue = -1;
    private const int ThrottleMs = 50;

    public void Start(string? status = null)
    {
        IsRunning = true;
        _lastReportedValue = -1;
        _throttleWatch.Restart();
        OnStart?.Invoke();
        if (!string.IsNullOrWhiteSpace(status))
            OnStatus?.Invoke(status);
    }

    public void SetProgress(int value)
    {
        if (!IsRunning) return;
        value = Math.Clamp(value, 0, 100);
        if (value == _lastReportedValue) return;
        if (_throttleWatch.ElapsedMilliseconds < ThrottleMs && value < 100) return;
        _lastReportedValue = value;
        _throttleWatch.Restart();
        OnProgress?.Invoke(value);
    }

    public void SetStatus(string text)
    {
        if (IsRunning && !string.IsNullOrWhiteSpace(text))
            OnStatus?.Invoke(text);
    }

    public void Stop()
    {
        if (!IsRunning) return;
        IsRunning = false;
        _throttleWatch.Stop();
        OnStop?.Invoke();
    }

    public void Reset()
    {
        IsRunning = false;
        _lastReportedValue = -1;
        _throttleWatch.Reset();
    }
}