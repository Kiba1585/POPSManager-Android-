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

        // 🔥 CACHE EN MEMORIA
        private Dictionary<string, string> _cfgIndex = new(StringComparer.OrdinalIgnoreCase);
        private bool _indexBuilt = false;

        // 🔥 MIRRORS DE COVERS (orden de prioridad)
        private static readonly string[] CoverMirrors = new[]
        {
            "https://raw.githubusercontent.com/Luden02/oplm-art/main/ART",
            "https://raw.githubusercontent.com/xlenore/psx-covers/main/covers",
            "https://archive.org/download/oplm-art-2023-11/ART"  // fallback
        };

        public GameAssetService(IPathsService paths, ILoggingService log,
            GameListService listService, DatabaseUpdaterService dbUpdater)
        {
            _paths = paths;
            _log = log;
            _listService = listService;
            _dbUpdater = dbUpdater;
        }

        // ==================== INDEX CFG ====================

        private void BuildCfgIndex()
        {
            if (_indexBuilt) return;

            string sourceCfg = Path.Combine(InternalDatabaseFolder, "CFG");
            _log.Log($"[CFG] Construyendo índice desde: {sourceCfg}");

            if (!Directory.Exists(sourceCfg))
            {
                _log.Log("[CFG] ERROR: carpeta no existe");
                return;
            }

            var files = Directory.GetFiles(sourceCfg, "*.cfg");
            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file).Trim();
                string normalized = Normalize(name);

                if (!_cfgIndex.ContainsKey(name))
                    _cfgIndex[name] = file;
                if (!_cfgIndex.ContainsKey(normalized))
                    _cfgIndex[normalized] = file;
            }

            _log.Log($"[CFG] Índice construido: {_cfgIndex.Count} entradas");
            _indexBuilt = true;
        }

        private static string Normalize(string id)
        {
            return id?
                .Replace(".", "")
                .Replace("-", "")
                .Replace(" ", "")
                .Trim()
                .ToUpperInvariant() ?? "";
        }

        // ==================== MODOS DE ACTUALIZACIÓN ====================

        public async Task<string> CheckAndUpdateFullAsync(Action<string> onProgress)
        {
            var (newAvailable, tag) = await _dbUpdater.CheckForUpdateAsync();
            if (!newAvailable || string.IsNullOrWhiteSpace(tag))
            {
                onProgress("Base de datos ya actualizada.");
                return "Base de datos actualizada.";
            }

            onProgress($"Nueva versión: {tag}. Descargando base completa...");
            bool ok = await _dbUpdater.DownloadFullDatabaseAsync(onProgress);
            if (ok)
            {
                _dbUpdater.SaveVersion(tag);
                GameDatabase.Initialize(InternalDatabaseFolder);
                _indexBuilt = false;
                _cfgIndex.Clear();
                return "Base de datos completa actualizada.";
            }
            return "Error al actualizar la base de datos completa.";
        }

        public async Task<string> UpdateIndividualAsync(Action<string> onProgress)
        {
            var (newAvailable, tag) = await _dbUpdater.CheckForUpdateAsync();
            if (!newAvailable || string.IsNullOrWhiteSpace(tag))
            {
                onProgress("Base de datos ya actualizada.");
                return "Base de datos actualizada.";
            }

            var allIds = _listService.Ps1Games.Select(g => g.GameId)
                         .Concat(_listService.Ps2Games.Select(g => g.GameId))
                         .Where(id => !string.IsNullOrWhiteSpace(id))
                         .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (allIds.Count == 0)
            {
                onProgress("No hay juegos detectados. Descargue la base completa.");
                return "No hay juegos.";
            }

            onProgress($"Nueva versión: {tag}. Descargando metadatos de {allIds.Count} juegos...");
            bool ok = await _dbUpdater.DownloadIndividualDatabaseAsync(allIds, onProgress);
            if (ok)
            {
                _dbUpdater.SaveVersion(tag);
                GameDatabase.Initialize(InternalDatabaseFolder);
                _indexBuilt = false;
                _cfgIndex.Clear();
                return "Metadatos de juegos detectados actualizados.";
            }
            return "Error al actualizar metadatos individuales.";
        }

        // ==================== METADATOS ====================

        public async Task<string> CopyMetadataAsync(Action<string> onProgress)
        {
            var all = _listService.Ps1Games.Concat(_listService.Ps2Games).ToList();
            if (!all.Any()) return "No hay juegos.";

            string cfgFolder = _paths.CfgFolder;
            if (!TestWrite(cfgFolder)) return $"❌ Sin permisos en CFG:\n{cfgFolder}";

            BuildCfgIndex();

            int copied = 0, skipped = 0, notFound = 0;

            for (int i = 0; i < all.Count; i++)
            {
                var g = all[i];
                string origId = g.OriginalGameId?.Trim() ?? "";
                string normId = Normalize(g.GameId);

                onProgress($"{i + 1}/{all.Count}: {g.Name} ({origId})");

                if (string.IsNullOrWhiteSpace(origId))
                {
                    _log.Log($"[Meta] Sin ID: {g.Name}");
                    continue;
                }

                string dest = Path.Combine(cfgFolder, origId + ".cfg");
                if (File.Exists(dest))
                {
                    skipped++;
                    continue;
                }

                if (_cfgIndex.TryGetValue(origId, out var file) ||
                    _cfgIndex.TryGetValue(normId, out file))
                {
                    try
                    {
                        File.Copy(file, dest, true);
                        copied++;
                        _log.Log($"[Meta] OK: {origId}");
                    }
                    catch (Exception ex)
                    {
                        _log.Log($"[Meta][ERROR] {ex.Message}");
                    }
                }
                else
                {
                    _log.Log($"[Meta] NO ENCONTRADO: {origId} / {normId}");
                    notFound++;
                }
            }

            return $"Metadatos: {copied} copiados, {skipped} existían, {notFound} no encontrados.";
        }

        // ==================== COVERS (CON MIRRORS ALTERNATIVOS) ====================

        public async Task<string> DownloadCoversAsync(Action<string> onProgress)
        {
            var all = _listService.Ps1Games.Concat(_listService.Ps2Games).ToList();
            if (!all.Any()) return "No hay juegos.";

            string artFolder = _paths.ArtFolder;
            if (!TestWrite(artFolder)) return $"❌ Sin permisos en ART:\n{artFolder}";

            int downloaded = 0, skipped = 0, failed = 0;

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

            for (int i = 0; i < all.Count; i++)
            {
                var g = all[i];
                string origId = g.OriginalGameId?.Trim();
                string normId = Normalize(g.GameId);

                onProgress($"{i + 1}/{all.Count}: {g.Name}");

                if (string.IsNullOrWhiteSpace(origId))
                    continue;

                string artFile = Path.Combine(artFolder, origId + ".jpg");
                if (File.Exists(artFile) && new FileInfo(artFile).Length > 1000)
                {
                    skipped++;
                    continue;
                }

                bool success = false;

                // Probar cada mirror con normId y origId
                foreach (var mirrorBase in CoverMirrors)
                {
                    if (await TryDownloadWithRetry(client, $"{mirrorBase}/{normId}.jpg", artFile) ||
                        await TryDownloadWithRetry(client, $"{mirrorBase}/{origId}.jpg", artFile))
                    {
                        success = true;
                        break;
                    }
                }

                // Si ningún mirror funcionó, probar base de datos local
                if (!success)
                {
                    string? dbUrl = GameDatabase.TryGetCoverUrl(origId);
                    if (!string.IsNullOrWhiteSpace(dbUrl))
                        success = await TryDownloadWithRetry(client, dbUrl, artFile);
                }

                if (success)
                {
                    try
                    {
                        ArtResizer.ResizeToArt(
                            artFile,
                            artFile.Replace(".jpg", ".ART"),
                            msg => _log.Log(msg));
                    }
                    catch { }

                    downloaded++;
                }
                else
                {
                    _log.Log($"[Cover] FALLÓ: {origId}");
                    failed++;
                }
            }

            return $"Covers: {downloaded} desc, {skipped} existen, {failed} no encontrados.";
        }

        private async Task<bool> TryDownloadWithRetry(HttpClient client, string url, string dest, int maxRetries = 2)
        {
            for (int retry = 0; retry <= maxRetries; retry++)
            {
                try
                {
                    var res = await client.GetAsync(url);
                    if (res.IsSuccessStatusCode)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        await using var fs = new FileStream(dest, FileMode.Create);
                        await res.Content.CopyToAsync(fs);
                        return true;
                    }
                    _log.Log($"[HTTP] {url} -> {(int)res.StatusCode}");
                }
                catch (Exception ex)
                {
                    _log.Log($"[HTTP][ERR] {ex.Message} (intento {retry + 1})");
                }

                if (retry < maxRetries)
                    await Task.Delay(1000);  // Pequeño retardo antes de reintentar
            }
            return false;
        }

        // ==================== DIAGNÓSTICO ====================

        public string GetDebugIds()
        {
            BuildCfgIndex();

            var games = _listService.Ps1Games
                .Take(5)
                .Select(g => $"{g.Name} -> {g.OriginalGameId}");

            var files = _cfgIndex.Keys.Take(5);

            return $"🎮 Juegos:\n{string.Join("\n", games)}\n\n📄 CFGs en caché:\n{string.Join("\n", files)}";
        }

        // ==================== AUXILIARES ====================

        private static bool TestWrite(string folder)
        {
            try
            {
                Directory.CreateDirectory(folder);
                string tmp = Path.Combine(folder, ".test");
                File.WriteAllText(tmp, "ok");
                File.Delete(tmp);
                return true;
            }
            catch { return false; }
        }
    }
}