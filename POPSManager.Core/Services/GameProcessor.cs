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
        // Ejecutar la detección en un hilo de fondo para evitar bloquear la UI
        return await Task.Run(() =>
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
        });
    }

    public async Task ProcessGamesAsync(string folder, IList<GameProgressItem> games)
    {
        foreach (var game in games)
        {
            game.Status = "Procesando...";
            _log.Log($"Procesando: {game.Name}");

            // Simulación de trabajo real
            await Task.Delay(300);

            game.Status = "Completado";
            _log.Log($"Completado: {game.Name}");
        }

        _notify.Show("Procesamiento completado.");
    }
}