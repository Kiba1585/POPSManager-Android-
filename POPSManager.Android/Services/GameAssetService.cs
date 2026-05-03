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

        public GameAssetService(IPathsService paths, ILoggingService log, GameListService listService)
        {
            _paths = paths;
            _log = log;
            _listService = listService;
        }

        /// <summary>
        /// Actualiza la base de datos interna, mostrando progreso mediante <paramref name="onProgress"/>.
        /// </summary>
        public async Task<string> UpdateDatabaseAsync(Action<string> onProgress)
        {
            var ids = _listService.Ps1Games.Select(g => g.OriginalGameId)
                      .Concat(_listService.Ps2Games.Select(g => g.OriginalGameId))
                      .Where(id => !string.IsNullOrWhiteSpace(id));

            bool ok = await DatabaseUpdater.DownloadAndExtractDatabaseAsync(_paths.RootFolder, ids, onProgress);
            if (ok)
            {
                GameDatabase.Initialize(InternalDatabaseFolder);
                return "Base de datos actualizada.";
            }
            return "Error al actualizar la base de datos.";
        }

        /// <summary>
        /// Descarga covers y copia metadatos, mostrando progreso mediante <paramref name="onProgress"/>.
        /// </summary>
        public async Task<string> DownloadCoversAndMetadataAsync(Action<string> onProgress)
        {
            var all = _listService.Ps1Games.Concat(_listService.Ps2Games).ToList();
            if (!all.Any()) return "No hay juegos.";

            string artFolder = _paths.ArtFolder;
            string cfgFolder = _paths.CfgFolder;
            bool canWriteArt = TestWrite(artFolder);
            bool canWriteCfg = TestWrite(cfgFolder);
            if (!canWriteArt && !canWriteCfg) return "❌ Sin permisos en ART ni CFG.";

            string sourceCfg = Path.Combine(InternalDatabaseFolder, "CFG");
            bool canMetadata = Directory.Exists(sourceCfg);
            int cacheCount = 0;
            string cacheSample = "";
            if (canMetadata)
            {
                var files = Directory.GetFiles(sourceCfg, "*.cfg");
                cacheCount = files.Length;
                cacheSample = files.FirstOrDefault() ?? "";
            }

            int coversDl = 0, coversSkip = 0, metaCopy = 0, metaSkip = 0;
            string mirror = "https://archive.org/download/oplm-art-2023-11";

            for (int i = 0; i < all.Count; i++)
            {
                var g = all[i];
                onProgress($"{i + 1}/{all.Count}: {g.Name}");

                if (string.IsNullOrWhiteSpace(g.OriginalGameId))
                {
                    _log.Log($"[SKIP] Sin ID: {g.Name}");
                    continue;
                }

                // Cover
                if (canWriteArt)
                {
                    string art = Path.Combine(artFolder, g.OriginalGameId + ".jpg");
                    if (!File.Exists(art) || new FileInfo(art).Length < 1000)
                    {
                        string url = GameDatabase.TryGetCoverUrl(g.OriginalGameId) ?? $"{mirror}/ART/{g.OriginalGameId}.jpg";
                        if (await DownloadFileAsync(url, art))
                        {
                            try { ArtResizer.ResizeToArt(art, art.Replace(".jpg", ".ART"), msg => _log.Log(msg)); }
                            catch { }
                            coversDl++;
                        }
                        else _log.Log($"[Cover] Falló {url}");
                    }
                    else coversSkip++;
                }

                // Metadata
                if (canWriteCfg && canMetadata)
                {
                    string dest = Path.Combine(cfgFolder, g.OriginalGameId + ".cfg");
                    if (!File.Exists(dest))
                    {
                        string? copied = TryCopyCfg(sourceCfg, cfgFolder, g.OriginalGameId)
                                     ?? TryCopyCfg(sourceCfg, cfgFolder, g.GameId);
                        if (copied != null) metaCopy++;
                        else _log.Log($"[Meta] No encontrado: {g.OriginalGameId}");
                    }
                    else metaSkip++;
                }
            }

            string msg = $"Covers: {coversDl} desc, {coversSkip} ya existían.\n";
            if (canMetadata) msg += $"Metadatos: {metaCopy} copiados, {metaSkip} ya existían.\n";
            else msg += "Metadatos: BD interna no disponible.\n";
            msg += $"Caché: {cacheCount} CFGs. Ej: {Path.GetFileName(cacheSample)}";
            return msg;
        }

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