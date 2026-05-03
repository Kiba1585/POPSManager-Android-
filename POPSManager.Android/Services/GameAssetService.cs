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

        // ==================== COVERS (CORREGIDO) ====================

        public async Task<string> DownloadCoversAsync(Action<string> onProgress)
        {
            var all = _listService.Ps1Games.Concat(_listService.Ps2Games).ToList();
            if (!all.Any()) return "No hay juegos.";

            string artFolder = _paths.ArtFolder;
            if (!TestWrite(artFolder)) return $"❌ Sin permisos en ART:\n{artFolder}";

            int downloaded = 0, skipped = 0, failed = 0;
            string mirrorBase = "https://archive.org/download/oplm-art-2023-11";

            for (int i = 0; i < all.Count; i++)
            {
                var g = all[i];
                string normId = g.GameId;            // sin punto (ej. SCES_00967)
                string origId = g.OriginalGameId;    // con punto (ej. SCES_009.67)

                onProgress($"{i + 1}/{all.Count}: {g.Name} (ID: {normId})");

                if (string.IsNullOrWhiteSpace(normId))
                {
                    _log.Log($"[Cover] Sin ID: {g.Name}");
                    continue;
                }

                // 1. Primero intentar con el mirror usando el ID normalizado (sin punto)
                string artFile = Path.Combine(artFolder, normId + ".jpg");
                if (!File.Exists(artFile) || new FileInfo(artFile).Length < 1000)
                {
                    bool success = false;

                    // Intentar con el mirror (ID normalizado)
                    string url = $"{mirrorBase}/ART/{normId}.jpg";
                    if (await DownloadFileAsync(url, artFile))
                    {
                        success = true;
                    }
                    else
                    {
                        // 2. Si falla, intentar con la URL de la base de datos (puede tener el ID original)
                        string? dbUrl = GameDatabase.TryGetCoverUrl(origId);
                        if (!string.IsNullOrWhiteSpace(dbUrl))
                        {
                            if (await DownloadFileAsync(dbUrl, artFile))
                                success = true;
                        }
                    }

                    if (success)
                    {
                        try { ArtResizer.ResizeToArt(artFile, artFile.Replace(".jpg", ".ART"), msg => _log.Log(msg)); }
                        catch { }
                        downloaded++;
                    }
                    else
                    {
                        _log.Log($"[Cover] No disponible para {normId} (probado también como {origId})");
                        failed++;
                    }
                }
                else skipped++;
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
                string searchId = g.GameId;
                onProgress($"{i + 1}/{all.Count}: {g.Name} (ID: {searchId})");

                if (string.IsNullOrWhiteSpace(searchId))
                {
                    _log.Log($"[Meta] Sin ID: {g.Name}");
                    continue;
                }

                string dest = Path.Combine(cfgFolder, searchId + ".cfg");
                if (!File.Exists(dest))
                {
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