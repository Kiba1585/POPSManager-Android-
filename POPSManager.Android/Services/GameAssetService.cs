using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using POPSManager.Core.Logic;
using POPSManager.Core.Logic.Covers;
using POPSManager.Core.Services;
using static POPSManager.Android.Services.DatabaseUpdater;

namespace POPSManager.Android.Services
{
    public class GameAssetService
    {
        private readonly IPathsService _paths;
        private readonly ILoggingService _log;
        private readonly GameListService _listService;
        private readonly DatabaseUpdaterService _dbUpdater;

        private Dictionary<string, string> _cfgIndex = new(StringComparer.OrdinalIgnoreCase);
        private bool _indexBuilt = false;

        private static readonly string[] CoverMirrors = new[]
        {
            "https://raw.githubusercontent.com/Luden02/oplm-art/main/ART",
            "https://raw.githubusercontent.com/xlenore/psx-covers/main/covers",
            "https://archive.org/download/oplm-art-2023-11/ART"
        };

        public GameAssetService(IPathsService paths, ILoggingService log,
            GameListService listService, DatabaseUpdaterService dbUpdater)
        {
            _paths = paths;
            _log = log;
            _listService = listService;
            _dbUpdater = dbUpdater;
        }

        // ==================== ÍNDICE DE CFG ====================
        private void BuildCfgIndex()
        {
            if (_indexBuilt) return;

            string sourceCfg = Path.Combine(InternalDatabaseFolder, "CFG");
            _log.Log($"[CFG] Construyendo índice desde: {sourceCfg}");
            _log.Log($"[CFG] ¿Existe la carpeta?: {Directory.Exists(sourceCfg)}");

            if (!Directory.Exists(sourceCfg))
            {
                _log.Log("[CFG] ERROR: la carpeta CFG no existe en la caché interna.");
                return;
            }

            var files = Directory.GetFiles(sourceCfg, "*.cfg", SearchOption.AllDirectories);
            _log.Log($"[CFG] Archivos encontrados: {files.Length}");

            foreach (var f in files.Take(10))
                _log.Log($"[CFG] Archivo: {f}");

            _cfgIndex.Clear();
            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file).Trim();
                string normalized = Normalize(name);

                if (!_cfgIndex.ContainsKey(name))
                    _cfgIndex[name] = file;
                if (!_cfgIndex.ContainsKey(normalized))
                    _cfgIndex[normalized] = file;