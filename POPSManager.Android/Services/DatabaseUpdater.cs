using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace POPSManager.Android.Services
{
    public static class DatabaseUpdater
    {
        private const string Owner = "Kiba1585";
        private const string Repo = "POPSManager.DBGenerator";
        private const string DbVersionKey = "db_version";
        private const string DefaultDownloadUrl =
            $"https://github.com/{Owner}/{Repo}/releases/latest/download/POPSManager_DB.zip";
        private const string ApiUrl =
            $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";

        /// <summary>
        /// Descarga la base de datos y extrae solo los CFG de los juegos indicados.
        /// </summary>
        public static async Task<bool> DownloadAndExtractDatabaseAsync(
            string oplRootFolder,
            IEnumerable<string> gameIds,
            Action<string>? onProgress = null)
        {
            try
            {
                onProgress?.Invoke("Verificando versión de la base de datos...");

                (string? tag, string? downloadUrl) = await GetLatestReleaseInfoAsync();
                if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(downloadUrl))
                {
                    onProgress?.Invoke("No se pudo obtener información del release.");
                    return false;
                }

                string? savedVersion = Preferences.Get(DbVersionKey, null);
                if (savedVersion == tag)
                {
                    onProgress?.Invoke("La base de datos ya está actualizada.");
                    return true;
                }

                onProgress?.Invoke($"Descargando base de datos ({tag})...");

                string zipTemp = Path.Combine(Path.GetTempPath(), $"popsmanager_db_{Guid.NewGuid():N}.zip");
                if (File.Exists(zipTemp))
                {
                    try { File.Delete(zipTemp); } catch { }
                }

                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                client.DefaultRequestHeaders.Add("User-Agent", "POPSManager-Android");

                var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                using var stream = await response.Content.ReadAsStreamAsync();

                using (var fileStream = new FileStream(zipTemp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        if (totalBytes > 0)
                        {
                            int percent = (int)((totalRead * 100) / totalBytes);
                            onProgress?.Invoke($"Descargando... {percent}%");
                        }
                    }
                }

                onProgress?.Invoke("Filtrando metadatos...");

                // Extraer solo los CFG necesarios en una carpeta temporal
                string tempExtract = Path.Combine(Path.GetTempPath(), $"popsmanager_extract_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempExtract);

                await Task.Run(() =>
                {
                    if (File.Exists(zipTemp))
                    {
                        ZipFile.ExtractToDirectory(zipTemp, tempExtract, true);
                    }
                });

                // Copiar únicamente los CFG correspondientes a los juegos
                string cfgFolder = Path.Combine(oplRootFolder, "CFG");
                Directory.CreateDirectory(cfgFolder);

                var gameIdSet = new HashSet<string>(gameIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);
                int copied = 0;

                string sourceCfgFolder = Path.Combine(tempExtract, "CFG");
                if (Directory.Exists(sourceCfgFolder))
                {
                    foreach (string cfgFile in Directory.GetFiles(sourceCfgFolder, "*.cfg"))
                    {
                        string cfgId = Path.GetFileNameWithoutExtension(cfgFile);
                        if (gameIdSet.Contains(cfgId))
                        {
                            string dest = Path.Combine(cfgFolder, Path.GetFileName(cfgFile));
                            File.Copy(cfgFile, dest, true);
                            copied++;
                        }
                    }
                }

                // Limpiar temporales
                try { File.Delete(zipTemp); } catch { }
                try { Directory.Delete(tempExtract, true); } catch { }

                Preferences.Set(DbVersionKey, tag);
                onProgress?.Invoke($"Base de datos actualizada. {copied} metadatos copiados.");
                return true;
            }
            catch (IOException ioEx) when (ioEx.Message.Contains("SharingViolation"))
            {
                onProgress?.Invoke("El archivo temporal está en uso. Inténtalo de nuevo en unos segundos.");
                return false;
            }
            catch (Exception ex)
            {
                onProgress?.Invoke($"Error: {ex.Message}");
                return false;
            }
        }

        private static async Task<(string? tag, string? downloadUrl)> GetLatestReleaseInfoAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                client.DefaultRequestHeaders.Add("User-Agent", "POPSManager-Android");
                var json = await client.GetStringAsync(ApiUrl);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string? tag = root.GetProperty("tag_name").GetString();

                var assets = root.GetProperty("assets").EnumerateArray();
                string? downloadUrl = assets
                    .FirstOrDefault(a => a.GetProperty("name").GetString() == "POPSManager_DB.zip")
                    .GetProperty("browser_download_url")
                    .GetString();

                return (tag, downloadUrl);
            }
            catch
            {
                return ("unknown", DefaultDownloadUrl);
            }
        }
    }
}