using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using POPSManager.Core.Logic;
using POPSManager.Core.Services;
using POPSManager.Android.Models;

namespace POPSManager.Android.Services
{
    public class GameProcessingService
    {
        private readonly IPathsService _paths;
        private readonly ILoggingService _log;
        private readonly GameListService _listService;

        public GameProcessingService(IPathsService paths, ILoggingService log, GameListService listService)
        {
            _paths = paths;
            _log = log;
            _listService = listService;
        }

        public async Task<string> RenameAllAsync(bool cheatWidescreen, bool cheatNoPal, bool cheatFixSound, bool cheatFixGraphics)
        {
            var ps1 = _listService.Ps1Games;
            var ps2 = _listService.Ps2Games;
            if (!ps1.Any() && !ps2.Any()) return "No hay juegos para renombrar.";

            CreateMultidiscStructure(ps1);

            int renamed = 0, skipped = 0;
            var errors = new List<string>();

            foreach (var g in ps1.ToList())
            {
                try
                {
                    string folder = Path.GetDirectoryName(g.FilePath)!;
                    string suffix = "";
                    if (g.IsMultiDisc && g.DiscNumber > 1)
                        suffix = $" (CD{g.DiscNumber})";
                    string newName = $"{g.OriginalGameId}.{g.Name}{suffix}.VCD";
                    string newPath = Path.Combine(folder, newName);

                    if (string.Equals(g.FilePath, newPath, StringComparison.OrdinalIgnoreCase))
                    { skipped++; continue; }

                    File.Move(g.FilePath, newPath);
                    g.FilePath = newPath;

                    if (Directory.Exists(g.GameFolder))
                    {
                        string newFolder = Path.Combine(folder, $"{g.OriginalGameId}.{g.Name}{suffix}");
                        Directory.Move(g.GameFolder, newFolder);
                        g.GameFolder = newFolder;
                    }
                    renamed++;
                }
                catch (Exception ex) { errors.Add($"{g.Name}: {ex.Message}"); }
            }

            foreach (var g in ps2.ToList())
            {
                try
                {
                    string folder = Path.GetDirectoryName(g.FilePath)!;
                    string suffix = g.IsMultiDisc && g.DiscNumber > 1 ? $" (CD{g.DiscNumber})" : "";
                    string newName = $"{g.OriginalGameId}.{g.Name}{suffix}.iso";
                    string newPath = Path.Combine(folder, newName);

                    if (string.Equals(g.FilePath, newPath, StringComparison.OrdinalIgnoreCase))
                    { skipped++; continue; }

                    File.Move(g.FilePath, newPath);
                    g.FilePath = newPath;
                    if (Directory.Exists(g.GameFolder))
                    {
                        string newFolder = Path.Combine(folder, $"{g.OriginalGameId}.{g.Name}{suffix}");
                        Directory.Move(g.GameFolder, newFolder);
                        g.GameFolder = newFolder;
                    }
                    renamed++;
                }
                catch (Exception ex) { errors.Add($"{g.Name}: {ex.Message}"); }
            }

            _listService.Refresh();

            string msg = $"Renombrados: {renamed}. Omitidos: {skipped} (ya tenían el formato).";
            if (errors.Any()) msg += $" Errores: {string.Join("; ", errors)}";
            return msg;
        }

        // ... (GenerateAllElfsAsync, GenerateAllCheatsAsync, ProcessMultidiscAsync se mantienen igual que antes)

        private void CreateMultidiscStructure(ObservableCollection<GameItem> ps1Games)
        {
            // 1. Agrupar por Game ID normalizado
            var groups = ps1Games
                .Where(g => !string.IsNullOrWhiteSpace(g.GameId))
                .GroupBy(g => g.GameId, StringComparer.OrdinalIgnoreCase)
                .Where(grp => grp.Count() > 1);

            foreach (var grp in groups)
            {
                var discs = grp.OrderBy(g => g.DiscNumber).ToList();
                string baseName = discs.First().Name;
                baseName = Regex.Replace(baseName, @"\s*\(CD\d\)", "").Trim();
                string commonFolder = Path.Combine(_paths.PopsFolder, baseName);
                Directory.CreateDirectory(commonFolder);

                string txt = Path.Combine(commonFolder, "DISCS.TXT");
                if (!File.Exists(txt))
                    File.WriteAllLines(txt, discs.Select(d => Path.GetFileName(d.FilePath)));

                for (int i = 0; i < discs.Count; i++)
                {
                    discs[i].GameFolder = commonFolder;
                    discs[i].IsMultiDisc = true;
                    if (discs[i].DiscNumber == 0) discs[i].DiscNumber = i + 1;
                }
            }

            // 2. Fallback por nombre base para los que aún no son multidisco
            var nameGroups = ps1Games
                .Where(g => !g.IsMultiDisc)
                .GroupBy(g =>
                {
                    string n = Path.GetFileNameWithoutExtension(g.FilePath);
                    n = Regex.Replace(n, @"\s*\((CD|Disc|Disco)\s*\d\)", "", RegexOptions.IgnoreCase);
                    n = Regex.Replace(n, @"\s*(CD|Disc|Disco)\s*\d", "", RegexOptions.IgnoreCase).Trim();
                    return n;
                }, StringComparer.OrdinalIgnoreCase)
                .Where(grp => grp.Count() > 1);

            foreach (var grp in nameGroups)
            {
                var files = grp.OrderBy(f => f.FilePath).ToList();
                string baseName = grp.Key;
                string commonFolder = Path.Combine(_paths.PopsFolder, baseName);
                Directory.CreateDirectory(commonFolder);

                string txt = Path.Combine(commonFolder, "DISCS.TXT");
                if (!File.Exists(txt))
                    File.WriteAllLines(txt, files.Select(f => Path.GetFileName(f.FilePath)));

                for (int i = 0; i < files.Count; i++)
                {
                    var game = ps1Games.FirstOrDefault(g => g.FilePath == files[i].FilePath);
                    if (game != null)
                    {
                        game.IsMultiDisc = true;
                        game.DiscNumber = i + 1;
                        game.GameFolder = commonFolder;
                    }
                }
            }
        }
    }
}