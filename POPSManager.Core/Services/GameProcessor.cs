using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using POPSManager.Core.Logic;
using POPSManager.Core.Logic.Automation;
using POPSManager.Core.Logic.Cheats;
using POPSManager.Core.Logic.Covers;
using POPSManager.Core.Models;
using POPSManager.Core.Services;
using POPSManager.Core.Settings;
using POPSManager.Core.Localization;

namespace POPSManager.Core.Services
{
    public sealed class GameProcessor
    {
        private readonly ProgressService _progress;
        private readonly LoggingService _log;
        private readonly NotificationService _notify;
        private readonly PathsService _paths;
        private readonly CheatSettingsService _cheatSettings;
        private readonly CheatManagerService _cheatManager;
        private readonly SettingsService _settings;
        private readonly AutomationEngine _auto;
        private readonly LocalizationService _loc;

        private static readonly int _maxCoverParallel = Math.Clamp(Environment.ProcessorCount * 2, 5, 10);
        private static readonly SemaphoreSlim _coverSemaphore = new(_maxCoverParallel, _maxCoverParallel);

        public GameProcessor(
            ProgressService progress,
            LoggingService log,
            NotificationService notify,
            PathsService paths,
            CheatSettingsService cheatSettings,
            CheatManagerService cheatManager,
            SettingsService settings,
            AutomationEngine auto,
            LocalizationService loc)
        {
            _progress = progress;
            _log = log;
            _notify = notify;
            _paths = paths;
            _cheatSettings = cheatSettings;
            _cheatManager = cheatManager;
            _settings = settings;
            _auto = auto;
            _loc = loc;
        }

        public void ProcessFolder(string folder) => ProcessFolderAsync(folder, null).GetAwaiter().GetResult();

        public async Task ProcessFolderAsync(string folder, ProgressViewModel? perGameProgress = null, CancellationToken ct = default)
        {
            if (!Directory.Exists(folder))
            {
                _notify.Error(_loc.GetString("GameProcessor_InvalidFolder"));
                return;
            }

            var files = Directory.GetFiles(folder)
                .Where(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f).ToArray();

            if (files.Length == 0)
            {
                _notify.Warning(_loc.GetString("GameProcessor_NoFilesFound"));
                return;
            }

            var groups = GroupByRealGame(files);
            var groupList = groups.ToList();
            int total = groupList.Count;
            int completed = 0;

            var options = new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = ct };

            try
            {
                await Parallel.ForEachAsync(groupList, options, async (group, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    string baseTitle = group.Key;
                    var discs = group.Value;
                    string gameIdForUi = baseTitle;

                    try
                    {
                        bool isPs1 = discs.Any(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase));

                        if (isPs1)
                        {
                            string firstDisc = discs.First();
                            string detectedId = GameIdDetector.DetectGameId(firstDisc) ?? GameIdDetector.DetectFromName(baseTitle) ?? "";
                            if (!string.IsNullOrWhiteSpace(detectedId)) gameIdForUi = detectedId;

                            perGameProgress?.AddGame(baseTitle, gameIdForUi);
                            perGameProgress?.UpdateStatus(gameIdForUi, _loc.GetString("Progress_Preparing"));

                            var valid = discs.Where(ValidateVcd).ToList();
                            if (valid.Count == 0)
                            {
                                _log.Warn(string.Format("[PS1] {0} {1}", _loc.GetString("GameProcessor_AllVcdInvalid"), baseTitle));
                                perGameProgress?.MarkError(gameIdForUi, _loc.GetString("GameProcessor_VcdInvalid"));
                            }
                            else
                            {
                                await ProcessPS1GroupAsync(baseTitle, valid, gameIdForUi, perGameProgress, token);
                                perGameProgress?.MarkCompleted(gameIdForUi);
                            }
                        }
                        else
                        {
                            string iso = discs.First();
                            string originalName = Path.GetFileNameWithoutExtension(iso);
                            string detectedId = GameIdDetector.DetectGameId(iso) ?? GameIdDetector.DetectFromName(originalName) ?? "";
                            if (!string.IsNullOrWhiteSpace(detectedId)) gameIdForUi = detectedId;

                            perGameProgress?.AddGame(originalName, gameIdForUi);
                            perGameProgress?.UpdateStatus(gameIdForUi, _loc.GetString("Progress_Preparing"));

                            if (!ValidateIso(iso))
                            {
                                _log.Warn(string.Format("[PS2] {0}: {1}", _loc.GetString("GameProcessor_InvalidIso"), iso));
                                perGameProgress?.MarkError(gameIdForUi, _loc.GetString("GameProcessor_IsoInvalid"));
                            }
                            else
                            {
                                await ProcessPS2Async(iso, gameIdForUi, perGameProgress, token);
                                perGameProgress?.MarkCompleted(gameIdForUi);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _log.Warn(string.Format("{0} {1}", _loc.GetString("GameProcessor_ProcessingCancelled"), baseTitle));
                        _notify.Warning(_loc.GetString("GameProcessor_ProcessingCancelled"));
                        perGameProgress?.MarkError(gameIdForUi, _loc.GetString("GameProcessor_Cancelled"));
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _log.Error(string.Format("{0} {1}: {2}", _loc.GetString("GameProcessor_ErrorProcessing"), baseTitle, ex.Message));
                        _notify.Error(string.Format("{0} {1}", _loc.GetString("GameProcessor_ErrorProcessing"), baseTitle));
                        perGameProgress?.MarkError(gameIdForUi, _loc.GetString("GameProcessor_ErrorInProcessing"));
                    }
                    finally
                    {
                        int done = Interlocked.Increment(ref completed);
                        _progress.SetProgress((int)((done / (double)total) * 100));
                    }
                });

                _progress.SetStatus(_loc.GetString("Label_Completed"));
                await CopyCustomAssetsAsync();
            }
            catch (OperationCanceledException)
            {
                _progress.SetStatus(_loc.GetString("GameProcessor_Cancelled"));
            }
        }

        public async Task ProcessSingleGameAsync(string filePath, string category)
        {
            if (!File.Exists(filePath))
            {
                _notify.Error($"El archivo {filePath} no existe.");
                return;
            }

            if (category == "PS2")
            {
                await ProcessPS2Async(filePath, Path.GetFileNameWithoutExtension(filePath), null, CancellationToken.None);
            }
            else
            {
                var group = GroupByRealGame(new[] { filePath });
                var kv = group.First();
                await ProcessPS1GroupAsync(kv.Key, kv.Value, kv.Key, null, CancellationToken.None);
            }
        }

        public async Task DownloadCoverAsync(string gameId, string category)
        {
            var dbEntry = GameDatabase.TryGetEntry(gameId, out var entry) ? entry : null;
            if (dbEntry?.CoverUrl == null)
            {
                _notify.Warning($"No se encontró URL de carátula para {gameId}.");
                return;
            }

            string artFolder = category == "PS2" ? Path.Combine(_paths.DvdFolder, "ART") : Path.Combine(_paths.PopsFolder, "ART");
            Directory.CreateDirectory(artFolder);

            await _coverSemaphore.WaitAsync();
            try
            {
                await ArtDownloader.DownloadArtAsync(gameId, dbEntry.CoverUrl, artFolder, _log.Info);
            }
            finally { _coverSemaphore.Release(); }
        }

        public async Task GenerateElfAsync(string gamePath, string gameId, string category)
        {
            if (category == "PS2")
            {
                _notify.Info("Los juegos de PS2 no requieren ELF.");
                return;
            }

            string vcdPath = Directory.Exists(gamePath)
                ? Directory.GetFiles(gamePath, "*.VCD").FirstOrDefault() ?? ""
                : gamePath;

            if (!File.Exists(vcdPath))
            {
                _notify.Error("No se encontró el archivo VCD.");
                return;
            }

            string title = Path.GetFileName(Path.GetDirectoryName(vcdPath)) ?? gameId;
            bool ok = ElfGenerator.GeneratePs1Elf(_paths.PopstarterElfPath, vcdPath, _paths.AppsFolder, 1, title, gameId, _log.Info);

            if (ok) _log.Info($"ELF generado para {gameId}.");
            else _notify.Error($"Error generando ELF para {gameId}.");
        }

        public async Task GenerateMetadataAsync(string gameId, string gamePath, string category)
        {
            var dbEntry = GameDatabase.TryGetEntry(gameId, out var entry) ? entry : null;
            string title = Path.GetFileName(gamePath) ?? gameId;
            GenerateMetadataFile(gameId, title, dbEntry);
        }

        public async Task GenerateCheatsAsync(string gameId, string gamePath, string category)
        {
            if (!GameIdDetector.IsPalRegion(gameId))
            {
                _notify.Info("Solo se generan cheats para juegos PAL.");
                return;
            }

            string cd1Folder = Directory.Exists(gamePath) ? Path.Combine(gamePath, "CD1") : Path.GetDirectoryName(gamePath) ?? "";
            CheatGenerator.GenerateCheatTxt(gameId, cd1Folder, _log.Info);
        }

        private async Task CopyCustomAssetsAsync()
        {
            if (_auto.ShouldCopyLng())
                await Task.Run(() => CopyCustomFolderContents(_paths.LngFolder, "LNG", _log.Info));
            else _log.Info("[AUTO] Copia de archivos LNG desactivada por automatizacion.");

            if (_auto.ShouldCopyThm())
                await Task.Run(() => CopyCustomFolderContents(_paths.ThmFolder, "THM", _log.Info));
            else _log.Info("[AUTO] Copia de temas THM desactivada por automatizacion.");
        }

        private Dictionary<string, List<string>> GroupByRealGame(string[] files)
        {
            var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string realTitle = NameCleanerBase.CleanTitleOnly(name);
                if (!groups.ContainsKey(realTitle)) groups[realTitle] = new List<string>();
                groups[realTitle].Add(file);
            }
            return groups;
        }

        private bool ValidateVcd(string vcdPath) => IntegrityValidator.Validate(vcdPath);

        private bool ValidateIso(string isoPath)
        {
            try { var info = new FileInfo(isoPath); return info.Exists && info.Length >= 100_000_000; }
            catch { return false; }
        }

        private async Task ProcessPS1GroupAsync(string baseName, List<string> discs, string gameIdForUi, ProgressViewModel? perGameProgress, CancellationToken ct)
        {
            _log.Info(string.Format("[PS1] Procesando: {0}", baseName));
            string firstDisc = discs.First();
            string detectedId = GameIdDetector.DetectGameId(firstDisc) ?? GameIdDetector.DetectFromName(baseName) ?? "";
            if (string.IsNullOrWhiteSpace(detectedId)) { _notify.Warning($"No se pudo detectar ID para {baseName}"); return; }

            bool genCheats = _auto.ShouldGenerateCheats();
            bool genElf = _auto.ShouldGenerateElf();

            string cleanTitle = NameCleanerBase.CleanTitleOnly(baseName);
            string gameRootFolder = Path.Combine(_paths.PopsFolder, cleanTitle);
            Directory.CreateDirectory(gameRootFolder);

            int discNumber = 1;
            foreach (var disc in discs)
            {
                ct.ThrowIfCancellationRequested();
                string discFolder = Path.Combine(gameRootFolder, $"CD{discNumber}");
                Directory.CreateDirectory(discFolder);
                string destVcd = Path.Combine(discFolder, $"{cleanTitle} (Disc {discNumber}).VCD");
                File.Copy(disc, destVcd, true);
                _log.Info($"Copiado disco {discNumber}: {destVcd}");
                discNumber++;
            }

            if (genCheats && GameIdDetector.IsPalRegion(detectedId))
                CheatGenerator.GenerateCheatTxt(detectedId, Path.Combine(gameRootFolder, "CD1"), _log.Info);

            if (genElf)
            {
                string cd1Folder = Path.Combine(gameRootFolder, "CD1");
                string? vcdPath = Directory.GetFiles(cd1Folder, "*.VCD").FirstOrDefault();
                if (vcdPath != null)
                    ElfGenerator.GeneratePs1Elf(_paths.PopstarterElfPath, vcdPath, _paths.AppsFolder, 1, cleanTitle, detectedId, _log.Info);
            }

            _notify.Success($"{cleanTitle} procesado correctamente.");
        }

        private async Task ProcessPS2Async(string isoPath, string gameIdForUi, ProgressViewModel? perGameProgress, CancellationToken ct)
        {
            string originalName = Path.GetFileNameWithoutExtension(isoPath);
            string cleanTitle = NameCleanerBase.CleanTitleOnly(originalName);
            string dest = Path.Combine(_paths.DvdFolder, $"{cleanTitle}.ISO");
            Directory.CreateDirectory(_paths.DvdFolder);
            File.Copy(isoPath, dest, true);
            _log.Info($"PS2 copiado: {dest}");
            _notify.Success($"{cleanTitle} copiado a DVD correctamente.");
        }

        private void GenerateMetadataFile(string gameId, string title, GameEntry? dbEntry) { /* Placeholder */ }

        private void CopyCustomFolderContents(string sourceFolder, string folderName, Action<string> log) { /* Placeholder */ }
        private void CopyDirectoryRecursive(string source, string dest, Action<string> log) { /* Placeholder */ }
    }
}