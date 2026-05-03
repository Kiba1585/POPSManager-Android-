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

        /// <summary> Descarga las carátulas (covers) de los juegos listados. </summary>
        public async Task<string> DownloadCoversAsync(Action<string> onProgress)
        {
            var all = _listService.Ps1Games.Concat(_listService.Ps2Games).ToList();
            if (!all.Any()) return "No hay juegos.";

            string artFolder = _paths.ArtFolder;
            if (!TestWrite(artFolder)) return $"❌ Sin permisos de escritura en ART:\n{artFolder}";

            int downloaded = 0, skipped = 0;
            string mirror = "https://archive.org/download/oplm-art-2023-11";

            for (int i = 0; i < all.Count; i++)
            {
                var g = all[i];
                onProgress($"{i + 1}/{all.Count}: {g.Name}");

                if (string.IsNullOrWhiteSpace(g.OriginalGameId))
                {
                    _log.Log($"[Cover] Sin ID: {g.Name}");
                    continue;
                }

                string art = Path.Combine(artFolder, g.OriginalGameId + ".jpg");
                if (!File.Exists(art) || new FileInfo(art).Length < 1000)
                {
                    string url = GameDatabase.TryGetCoverUrl(g.OriginalGameId)
                                 ?? $"{mirror}/ART/{g.OriginalGameId}.jpg";
                    if (await DownloadFileAsync(url, art))
                    {
                        try { ArtResizer.ResizeToArt(art, art.Replace(".jpg", ".ART"), msg => _log.Log(msg)); }
                        catch { }
                        downloaded++;
                    }
                    else _log.Log($"[Cover] Falló {url}");
                }
                else skipped++;
            }

            return $"Covers: {downloaded} descargados, {skipped} ya existían.";
        }

        /// <summary> Copia los metadatos (.cfg) desde la caché interna a la carpeta CFG del destino. </summary>
        public async Task<string> CopyMetadataAsync(Action<string> onProgress)
        {
            var all = _listService.Ps1Games.Concat(_listService.Ps2Games).ToList();
            if (!all.Any()) return "No hay juegos.";

            string cfgFolder = _paths.CfgFolder;
            if (!TestWrite(cfgFolder)) return $"❌ Sin permisos de escritura en CFG:\n{cfgFolder}";

            string sourceCfg = Path.Combine(InternalDatabaseFolder, "CFG");
            if (!Directory.Exists(sourceCfg))
                return "La caché interna no existe. Usa 'Actualizar DB' primero.";

            int copied = 0, skipped = 0, notFound = 0;
            for (int i = 0; i < all.Count; i++)
            {
                var g = all[i];
                onProgress($"{i + 1}/{all.Count}: {g.Name}");

                if (string.IsNullOrWhiteSpace(g.OriginalGameId))
                {
                    _log.Log($"[Meta] Sin ID: {g.Name}");
                    continue;
                }

                string dest = Path.Combine(cfgFolder, g.OriginalGameId + ".cfg");
                if (!File.Exists(dest))
                {
                    string? copiedFile = TryCopyCfg(sourceCfg, cfgFolder, g.OriginalGameId)
                                      ?? TryCopyCfg(sourceCfg, cfgFolder, g.GameId);
                    if (copiedFile != null)
                    {
                        copied++;
                    }
                    else
                    {
                        _log.Log($"[Meta] No encontrado: {g.OriginalGameId}");
                        notFound++;
                    }
                }
                else skipped++;
            }

            return $"Metadatos: {copied} copiados, {skipped} ya existían, {notFound} no encontrados.";
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