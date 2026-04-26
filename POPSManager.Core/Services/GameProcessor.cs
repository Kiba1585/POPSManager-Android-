using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using POPSManager.Core.Models;

namespace POPSManager.Core.Services;

public class GameProcessor
{
    private readonly ILoggingService _log;
    private readonly INotificationService _notify;
    private readonly ConverterService _converter;

    public GameProcessor(ILoggingService log, INotificationService notify, ConverterService converter)
    {
        _log = log;
        _notify = notify;
        _converter = converter;
    }

    public async Task<List<GameProgressItem>> DetectGamesAsync(string folder)
    {
        return await Task.Run(() =>
        {
            var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase))
                .Select(f => new GameProgressItem
                {
                    Name = Path.GetFileNameWithoutExtension(f),
                    Status = "Pendiente"
                })
                .ToList();

            return files;
        });
    }

    public async Task ProcessGamesAsync(string folder, IList<GameProgressItem> games, string outputFolder)
    {
        await _converter.ConvertFolderAsync(folder, outputFolder);

        foreach (var game in games)
        {
            game.Status = "Completado";
        }
    }
}