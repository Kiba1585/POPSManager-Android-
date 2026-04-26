using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace POPSManager.Core.Services;

public sealed class LoggingService : ILoggingService, IAsyncDisposable
{
    public Action<string>? OnLog { get; set; }

    private readonly string _logFilePath;
    private readonly Channel<string> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _writerTask;

    private const int MaxDaysToKeep = 7;

    public LoggingService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string logFolder = Path.Combine(appData, "POPSManager", "Logs");
        Directory.CreateDirectory(logFolder);

        _logFilePath = Path.Combine(logFolder, $"log_{DateTime.Now:yyyyMMdd}.txt");

        _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _writerTask = Task.Run(() => ProcessLogQueueAsync(_cts.Token));

        RotateOldLogs(logFolder);
    }

    public void Log(string message) => WriteInternal(message, "INFO");
    public void Info(string msg) => WriteInternal(msg, "INFO");
    public void Warn(string msg) => WriteInternal(msg, "WARN");
    public void Error(string msg) => WriteInternal(msg, "ERROR");

    private void WriteInternal(string message, string level)
    {
        string formatted = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
        OnLog?.Invoke(formatted);
        Debug.WriteLine(formatted);
        _channel.Writer.TryWrite(formatted);
    }

    private async Task ProcessLogQueueAsync(CancellationToken ct)
    {
        await foreach (var line in _channel.Reader.ReadAllAsync(ct))
        {
            await File.AppendAllTextAsync(_logFilePath, line + Environment.NewLine, Encoding.UTF8, ct);
        }
    }

    private static void RotateOldLogs(string logFolder)
    {
        var cutoff = DateTime.Now.AddDays(-MaxDaysToKeep);
        foreach (var file in Directory.GetFiles(logFolder, "log_*.txt"))
        {
            if (new FileInfo(file).CreationTime < cutoff)
                File.Delete(file);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        await _writerTask;
        _cts.Dispose();
    }
}