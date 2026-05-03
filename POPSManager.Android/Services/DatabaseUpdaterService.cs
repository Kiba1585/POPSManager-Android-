using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using POPSManager.Core.Services;
using static POPSManager.Android.Services.DatabaseUpdater;

namespace POPSManager.Android.Services
{
    public class DatabaseUpdaterService
    {
        private const string Owner = "Kiba1585";
        private const string Repo = "POPSManager.DBGenerator";
        private const string FullDbAssetName = "POPSManager_DB.zip";
        private const string IndividualDbAssetName = "POPSManager_DB_individual.zip";
        private const string VersionKey = "db_tag_name";
        private const string ApiUrl = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
        private const string DownloadBase = $"https://github.com/{Owner}/{Repo}/releases/latest/download/";

        private readonly IPathsService _paths;
        private readonly ILoggingService _log;

        public DatabaseUpdaterService(IPathsService paths, ILoggingService log)
        {
            _paths = paths;
            _log = log;
        }

        /// <summary> Comprueba si hay una nueva versión. </summary>
        public async Task<(bool newAvailable, string? tag)> CheckForUpdateAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                client.DefaultRequestHeaders.Add("User-Agent", "POPSManager-Android");
                var json = await client.GetStringAsync(ApiUrl);
                using var doc = JsonDocument.Parse(json);
                string? tag = doc.RootElement.GetProperty("tag_name").GetString();
                if (string.IsNullOrWhiteSpace(tag))
                    return (false, null);

                string? saved = Preferences.Get(VersionKey, null);
                return (saved != tag, tag);
            }
            catch (Exception ex)
            {
                _log.Log($"[DB] Error al verificar versión: {ex.Message}");
                return (false, null);
            }
        }

        public void SaveVersion(string tag) => Preferences.Set(VersionKey, tag);

        // ==================== MODO COMPLETO ====================
        public async Task<bool> DownloadFullDatabaseAsync(Action<string> onProgress)
        {
            onProgress("Descargando base de datos completa...");
            string url = $"{DownloadBase}{FullDbAssetName}";
            string zipTemp = Path.Combine(Path.GetTempPath(), $"db_full_{Guid.NewGuid():N}.zip");

            try
            {
                await DownloadFileAsync(url, zipTemp, onProgress);

                string destCfg = _paths.CfgFolder;
                Directory.CreateDirectory(destCfg);
                string internalDb = InternalDatabaseFolder;
                if (Directory.Exists(internalDb))
                    Directory.Delete(internalDb, true);
                Directory.CreateDirectory(internalDb);

                await Task.Run(() =>
                {
                    using var archive = ZipFile.OpenRead(zipTemp);
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.StartsWith("CFG/") && entry.FullName.EndsWith(".cfg"))
                        {
                            string destPath = Path.Combine(destCfg, entry.Name);
                            entry.ExtractToFile(destPath, true);
                        }
                        else if (entry.FullName == "ps1db.json" || entry.FullName == "ps2db.json")
                        {
                            string destPath = Path.Combine(internalDb, entry.Name);
                            entry.ExtractToFile(destPath, true);
                        }
                    }
                });

                File.Delete(zipTemp);
                onProgress("Base de datos completa instalada.");
                return true;
            }
            catch (Exception ex)
            {
                _log.Log($"[DB] Error modo completo: {ex.Message}");
                onProgress($"Error: {ex.Message}");
                return false;
            }
        }

        // ==================== MODO SOLO JUEGOS DETECTADOS ====================
        public async Task<bool> DownloadIndividualDatabaseAsync(IEnumerable<string> gameIds, Action<string> onProgress)
        {
            var idList = gameIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
            if (idList == null || idList.Count == 0)
            {
                onProgress("No se proporcionaron Game IDs.");
                return false;
            }

            onProgress("Descargando base de datos individual...");
            string url = $"{DownloadBase}{IndividualDbAssetName}";
            string zipTemp = Path.Combine(Path.GetTempPath(), $"db_indiv_{Guid.NewGuid():N}.zip");

            try
            {
                await DownloadFileAsync(url, zipTemp, onProgress);

                string destCfg = _paths.CfgFolder;
                Directory.CreateDirectory(destCfg);
                var gameIdSet = new HashSet<string>(idList, StringComparer.OrdinalIgnoreCase);

                await Task.Run(() =>
                {
                    using var archive = ZipFile.OpenRead(zipTemp);
                    var indexEntry = archive.GetEntry("index.json");
                    if (indexEntry != null)
                    {
                        using var indexStream = indexEntry.Open();
                        using var doc = JsonDocument.Parse(indexStream);
                        var root = doc.RootElement;
                        foreach (var prop in root.EnumerateObject())
                        {
                            string id = prop.Name;
                            if (!gameIdSet.Contains(id)) continue;

                            var info = prop.Value;
                            if (info.TryGetProperty("cfg", out var cfgProp))
                            {
                                string cfgRelPath = cfgProp.GetString();
                                var cfgEntry = archive.GetEntry(cfgRelPath);
                                if (cfgEntry != null)
                                {
                                    string dest = Path.Combine(destCfg, id + ".cfg");
                                    cfgEntry.ExtractToFile(dest, true);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Fallback: buscar en carpeta cfg/
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.FullName.StartsWith("cfg/") && entry.FullName.EndsWith(".cfg"))
                            {
                                string id = Path.GetFileNameWithoutExtension(entry.Name);
                                if (gameIdSet.Contains(id))
                                {
                                    string dest = Path.Combine(destCfg, entry.Name);
                                    entry.ExtractToFile(dest, true);
                                }
                            }
                        }
                    }
                });

                File.Delete(zipTemp);
                onProgress("Base de datos individual instalada.");
                return true;
            }
            catch (Exception ex)
            {
                _log.Log($"[DB] Error modo individual: {ex.Message}");
                onProgress($"Error: {ex.Message}");
                return false;
            }
        }

        private static async Task DownloadFileAsync(string url, string dest, Action<string> onProgress)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long total = response.Content.Headers.ContentLength ?? -1;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fs.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;
                if (total > 0)
                {
                    int pct = (int)(totalRead * 100 / total);
                    onProgress($"Descargando... {pct}%");
                }
            }
        }
    }
}