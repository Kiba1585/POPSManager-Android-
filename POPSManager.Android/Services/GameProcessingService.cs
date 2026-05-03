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

            // 1. Detectar y crear estructura multidisco
            CreateMultidiscStructure(ps1);

            // 2. Renombrar
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

        // ... (GenerateAllElfsAsync, GenerateAllCheatsAsync, ProcessMultidiscAsync se mantienen igual)

        /// <summary> Crea DISCS.TXT y asigna números de disco basándose en la agrupación inteligente de archivos VCD. </summary>
        private void CreateMultidiscStructure(ObservableCollection<GameItem> ps1Games)
        {
            var allVcds = Directory.GetFiles(_paths.PopsFolder, "*.VCD", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(_paths.PopsFolder, "*.vcd", SearchOption.TopDirectoryOnly))
                .ToList();

            // Agrupar por nombre base, eliminando cualquier sufijo de disco común
            var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in allVcds)
            {
                string fullName = Path.GetFileNameWithoutExtension(file);
                // Intentar extraer nombre base eliminando: (CD x), (Disc x), CD x, Disc x, (Disco x), etc.
                string baseName = Regex.Replace(fullName, @"\s*\((CD|Disc|Disco)\s*\d\)", "", RegexOptions.IgnoreCase);
                baseName = Regex.Replace(baseName, @"\s*(CD|Disc|Disco)\s*\d", "", RegexOptions.IgnoreCase).Trim();

                // Si después de limpiar no cambió, usar el nombre completo como base
                if (string.IsNullOrWhiteSpace(baseName) || baseName == fullName)
                    baseName = fullName;

                if (!groups.ContainsKey(baseName))
                    groups[baseName] = new List<string>();
                groups[baseName].Add(file);
            }

            // Para cada grupo con más de un archivo, crear la estructura multidisco
            foreach (var grp in groups.Where(g => g.Value.Count > 1))
            {
                var files = grp.Value.OrderBy(f => f).ToList();
                string commonFolder = Path.Combine(_paths.PopsFolder, grp.Key);
                Directory.CreateDirectory(commonFolder);

                string txt = Path.Combine(commonFolder, "DISCS.TXT");
                if (!File.Exists(txt))
                    File.WriteAllLines(txt, files.Select(Path.GetFileName));

                // Asignar número de disco según el orden
                for (int i = 0; i < files.Count; i++)
                {
                    var game = ps1Games.FirstOrDefault(g => g.FilePath.Equals(files[i], StringComparison.OrdinalIgnoreCase));
                    if (game != null)
                    {
                        game.IsMultiDisc = true;
                        game.DiscNumber = i + 1;
                        game.GameFolder = commonFolder;
                        _log.Log($"[Multidisco] {game.Name} asignado como disco {i + 1}");
                    }
                }
            }
        }
    }
}