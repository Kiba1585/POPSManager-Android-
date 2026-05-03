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

        // ==================== GENERAR ELFs ====================
        public async Task<string> GenerateAllElfsAsync()
        {
            string baseElf = _paths.PopstarterElfPath;
            string rootElf = Path.Combine(_paths.RootFolder, "POPSTARTER.ELF");
            string popsElf = Path.Combine(_paths.PopsFolder, "POPSTARTER.ELF");
            bool existsRoot = File.Exists(rootElf);
            bool existsPops = File.Exists(popsElf);
            if (!existsRoot && !existsPops)
                return $"POPSTARTER.ELF no encontrado.\nRaíz: {rootElf}\nPOPS: {popsElf}";

            string elfToUse = existsPops ? popsElf : rootElf;
            await ProcessMultidiscAsync();

            int generated = 0, skipped = 0;
            foreach (var g in _listService.Ps1Games)
            {
                if (!File.Exists(g.FilePath)) { _log.Log($"[ELF] No VCD: {g.FilePath}"); continue; }
                string elfName = $"{Path.GetFileNameWithoutExtension(g.FilePath)}.ELF";
                string elfPath = g.IsMultiDisc ? Path.Combine(g.GameFolder, elfName)
                    : Path.Combine(Path.Combine(_paths.AppsFolder, g.Name), elfName);
                if (!g.IsMultiDisc) Directory.CreateDirectory(Path.GetDirectoryName(elfPath)!);

                if (!File.Exists(elfPath))
                {
                    ElfGenerator.GeneratePs1Elf(elfToUse, g.FilePath, elfPath, g.DiscNumber, g.Name, g.OriginalGameId, msg => _log.Log(msg));
                    generated++;
                }
                else { _log.Log($"[ELF] Ya existe: {elfName}"); skipped++; }

                if (!g.IsMultiDisc)
                {
                    string cfg = Path.Combine(Path.GetDirectoryName(elfPath)!, "title.cfg");
                    if (!File.Exists(cfg))
                    {
                        try { File.WriteAllText(cfg, $"title={g.Name}\nboot={elfName}\n"); }
                        catch (Exception ex) { _log.Log($"[ELF] Error title.cfg: {ex.Message}"); }
                    }
                }
            }

            return generated > 0 ? $"{generated} ELFs generados. {skipped} ya existían." : $"No se generaron ELFs. {skipped} ya existían.";
        }

        // ==================== GENERAR CHEATS ====================
        public async Task<string> GenerateAllCheatsAsync(bool widescreen, bool nopal, bool fixSound, bool fixGraphics)
        {
            var extra = new List<string>();
            if (widescreen) extra.Add("WIDESCREEN=ON");
            if (nopal) extra.Add("$NOPAL");
            if (fixSound) extra.Add("FIXSOUND=ON");
            if (fixGraphics) extra.Add("FIXGRAPHICS=ON");

            int gen = 0, skip = 0;
            foreach (var g in _listService.Ps1Games)
            {
                Directory.CreateDirectory(g.GameFolder);
                string cheat = Path.Combine(g.GameFolder, "CHEAT.TXT");
                if (!File.Exists(cheat))
                {
                    CheatGenerator.GenerateCheatTxt(g.OriginalGameId, g.GameFolder, extra, msg => _log.Log(msg));
                    gen++;
                }
                else { _log.Log($"[Cheats] Ya existe: {cheat}"); skip++; }
            }
            return gen > 0 ? $"{gen} CHEAT.TXT generados. {skip} ya existían." : $"No se generaron cheats. {skip} ya existían.";
        }

        // ==================== PROCESAR MULTIDISCO ====================
        private async Task ProcessMultidiscAsync()
        {
            var groups = _listService.Ps1Games.Where(g => !string.IsNullOrEmpty(g.GameId))
                .GroupBy(g => new string(g.GameId.TakeWhile(c => c != '.' && c != '_').ToArray())).ToList();

            foreach (var grp in groups)
            {
                var discs = grp.OrderBy(d => d.DiscNumber).ToList();
                if (discs.Count <= 1) continue;

                string baseName = discs.First().Name;
                baseName = Regex.Replace(baseName, @"\s*\(CD\d\)", "").Trim();
                string common = Path.Combine(Path.GetDirectoryName(discs.First().FilePath)!, baseName);
                Directory.CreateDirectory(common);

                string txt = Path.Combine(common, "DISCS.TXT");
                if (!File.Exists(txt))
                {
                    var fileNames = discs.Select(d => Path.GetFileName(d.FilePath)).Where(f => f != null).Cast<string>().ToList();
                    await File.WriteAllLinesAsync(txt, fileNames);
                    _log.Log($"[Multidisco] Creado {txt}");
                }

                string baseElf = _paths.PopstarterElfPath;
                if (File.Exists(baseElf))
                {
                    foreach (var d in discs)
                    {
                        d.GameFolder = common;
                        string ef = Path.Combine(common, $"{Path.GetFileNameWithoutExtension(d.FilePath)}.ELF");
                        if (!File.Exists(ef))
                            ElfGenerator.GeneratePs1Elf(baseElf, d.FilePath, ef, d.DiscNumber, d.Name, d.OriginalGameId, msg => _log.Log(msg));
                    }
                }

                string tcfg = Path.Combine(common, "title.cfg");
                if (!File.Exists(tcfg))
                {
                    string efn = $"{Path.GetFileNameWithoutExtension(discs.First().FilePath)}.ELF";
                    await File.WriteAllTextAsync(tcfg, $"title={baseName}\nboot={efn}\n");
                }
            }
        }

        // ==================== CREAR ESTRUCTURA MULTIDISCO ====================
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