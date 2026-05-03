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

        public GameAssetService(IPathsService paths, ILoggingService log,
            GameListService listService, DatabaseUpdaterService dbUpdater)
        {
            _paths = paths;
            _log = log;
            _listService = listService;
            _dbUpdater = dbUpdater;
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
                return "Metadatos de juegos detectados actualizados.";
            }
            return "Error al actualizar metadatos individuales.";
        }

        // ==================== COVERS ====================

        public async Task<string> DownloadCoversAsync(Action<string> onProgress)
        {
            var all = _listService.Ps1Games.Concat(_listService.Ps2Games).ToList();
            if (!all.Any()) return "No hay juegos.";

            string artFolder = _paths.ArtFolder;
            if (!TestWrite(artFolder)) return $"❌ Sin permisos en ART:\n{artFolder}";

            int downloaded = 0, skipped = 0, failed = 0;
            string mirror = "https://archive.org/download/oplm-art-2023-11";

            for (int i = 0; i < all.Count; i++)
            {
                var g = all[i];
                string displayId = g.OriginalGameId;
                onProgress($"{i + 1}/{all.Count}: {g.Name} (ID: {displayId})");

                if (string.IsNullOrWhiteSpace(displayId))
                {
                    _log.Log($"[Cover] Sin ID: {g.Name}");
                    continue;
                }

                string art = Path.Combine(artFolder, displayId + ".jpg");
                if (File.Exists(art) && new FileInfo(art).Length >= 1000)
                {
                    skipped++;
                    continue;
                }

                // Intentar primero con URL de la base de datos (si existe)
                string? url = GameDatabase.TryGetCoverUrl(displayId);
                if (url == null)
                {
                    // Fallback al mirror público con el ID original (con punto)
                    url = $"{mirror}/ART/{displayId}.jpg";
                }

                if (await DownloadFileAsync(url, art))
                {
                    try { ArtResizer.ResizeToArt(art, art.Replace(".jpg", ".ART"), msg => _log.Log(msg)); }
                    catch { }
                    downloaded++;
                }
                else
                {
                    _log.Log($"[Cover] No disponible para {displayId}");
                    failed++;
                }
            }

            return $"Covers: {downloaded} desc, {skipped} existen, {failed} no encontrados.";
        }

        // ==================== METADATOS ====================

        public async Task<string> CopyMetadataAsync(Action<string> onProgress)
        {
            var all = _listService.Ps1Games.Concat(_listService.Ps2Games).ToList();
            if (!all.Any()) return "No hay juegos.";

            string cfgFolder = _paths.CfgFolder;
            if (!TestWrite(cfgFolder)) return $"❌ Sin permisos en CFG:\n{cfgFolder}";

            string sourceCfg = Path.Combine(InternalDatabaseFolder, "CFG");
            if (!Directory.Exists(sourceCfg))
                return "Caché interna no encontrada. Usa 'Actualizar BD'.";

            int copied = 0, skipped = 0, notFound = 0;
            for (int i = 0; i < all.Count; i++)
            {
                var g = all[i];
                string searchId = g.GameId; // ID normalizado (sin punto)
                onProgress($"{i + 1}/{all.Count}: {g.Name} (ID: {searchId})");

                if (string.IsNullOrWhiteSpace(searchId))
                {
                    _log.Log($"[Meta] Sin ID: {g.Name}");
                    continue;
                }

                string dest = Path.Combine(cfgFolder, searchId + ".cfg");
                if (!File.Exists(dest))
                {
                    // PRIMERO intentar con el ID normalizado (sin punto), DESPUÉS con el original
                    string? copiedFile = TryCopyCfg(sourceCfg, cfgFolder, searchId)
                                      ?? TryCopyCfg(sourceCfg, cfgFolder, g.OriginalGameId);
                    if (copiedFile != null)
                    {
                        copied++;
                        _log.Log($"[Meta] Copiado {searchId}.cfg");
                    }
                    else
                    {
                        _log.Log($"[Meta] No se encuentra {searchId}.cfg (buscado también como {g.OriginalGameId})");
                        notFound++;
                    }
                }
                else skipped++;
            }

            return $"Metadatos: {copied} copiados, {skipped} existían, {notFound} no encontrados.";
        }

        // ==================== AUXILIARES ====================

        private static string? TryCopyCfg(string srcDir, string destDir, string id)
        {
            string s = Path.Combine(srcDir, id + ".cfg");
            string d = Path.Combine(destDir, id + ".cfg");
            if (File.Exists(s)) { try { File.Copy(s, d); return d; } catch { } }
            return null;
        }

        private static bool TestWrite(string folder)
        {
            try
            {
                Directory.CreateDirectory(folder);
                string tmp = Path.Combine(folder, ".writetest");
                File.WriteAllText(tmp, "test");
                File.Delete(tmp);
                return true;
            }
            catch { return false; }
        }

        private static async Task<bool> DownloadFileAsync(string url, string dest)
        {
            try
            {
                using var c = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                var r = await c.GetAsync(url);
                if (!r.IsSuccessStatusCode) return false;
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                await using var fs = new FileStream(dest, FileMode.Create);
                await r.Content.CopyToAsync(fs);
                return true;
            }
            catch { return false; }
        }
    }
}