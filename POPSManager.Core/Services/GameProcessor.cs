using POPSManager.Core.Models;

namespace POPSManager.Core.Services;

public class GameProcessor
{
    private readonly ILoggingService _log;
    private readonly INotificationService _notify;

    public GameProcessor(ILoggingService log, INotificationService notify)
    {
        _log = log;
        _notify = notify;
    }

    public async Task<List<GameProgressItem>> DetectGamesAsync(string folder)
    {
        var files = Directory.GetFiles(folder, "*.bin", SearchOption.AllDirectories)
                             .Concat(Directory.GetFiles(folder, "*.iso", SearchOption.AllDirectories))
                             .ToList();

        var list = new List<GameProgressItem>();

        foreach (var file in files)
        {
            list.Add(new GameProgressItem
            {
                Name = NameCleaner.Clean(Path.GetFileNameWithoutExtension(file)),
                Status = "Pendiente"
            });
        }

        return list;
    }

    public async Task ProcessGamesAsync(string folder, IList<GameProgressItem> games)
    {
        foreach (var game in games)
        {
            game.Status = "Procesando...";
            await Task.Delay(300); // Simulación

            game.Status = "Completado";
        }

        _notify.Show("Procesamiento completado.");
    }
}
