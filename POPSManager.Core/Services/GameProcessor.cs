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
            LoggingService log,
            NotificationService notify,
            PathsService paths,
            CheatSettingsService cheatSettings,
            CheatManagerService cheatManager,
            SettingsService settings,
            AutomationEngine auto,
            LocalizationService loc)
        {
            _log = log;
            _notify = notify;
            _paths = paths;
            _cheatSettings = cheatSettings;
            _cheatManager = cheatManager;
            _settings = settings;
            _auto = auto;
            _loc = loc;
        }

        public async Task ProcessFolderAsync(string folder, CancellationToken ct = default)
        {
            if (!Directory.Exists(folder))
            {
                _notify.Error("La carpeta seleccionada no existe.");
                return;
            }

            var files = Directory.GetFiles(folder)
                .Where(f => f.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var file in files)
            {
                string category = file.EndsWith(".iso", StringComparison.OrdinalIgnoreCase) ? "PS2" : "PS1";
                await ProcessSingleGameAsync(file, category);
            }
        }

        public async Task ProcessSingleGameAsync(string filePath, string category)
        {
            if (!File.Exists(filePath)) return;

            if (category == "PS2")
            {
                string dest = Path.Combine(_paths.DvdFolder, Path.GetFileNameWithoutExtension(filePath) + ".ISO");
                Directory.CreateDirectory(_paths.DvdFolder);
                File.Copy(filePath, dest, true);
                _notify.Success($"Juego de PS2 copiado.");
            }
            else
            {
                string title = Path.GetFileNameWithoutExtension(filePath);
                string gameFolder = Path.Combine(_paths.PopsFolder, title);
                Directory.CreateDirectory(gameFolder);
                string dest = Path.Combine(gameFolder, title + ".VCD");
                File.Copy(filePath, dest, true);
                _notify.Success($"Juego de PS1 copiado.");
            }
        }

        public async Task DownloadCoverAsync(string gameId, string category) { /* Placeholder */ }
        public async Task GenerateElfAsync(string gamePath, string gameId, string category) { /* Placeholder */ }
        public async Task GenerateMetadataAsync(string gameId, string gamePath, string category) { /* Placeholder */ }
        public async Task GenerateCheatsAsync(string gameId, string gamePath, string category) { /* Placeholder */ }

        private Dictionary<string, List<string>> GroupByRealGame(string[] files) => files
            .GroupBy(f => Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}