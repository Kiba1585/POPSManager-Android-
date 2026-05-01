using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;   // para Preferences

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
        /// Descarga la base de datos solo si hay una versión más reciente que la guardada.
        /// </summary>
        public static async Task<bool> DownloadAndExtractDatabaseAsync(
            string oplRootFolder,
            Action<string>? onProgress = null)
        {
            try
            {
                onProgress?.Invoke("Verificando versión de la base de datos...");

                // 1. Obtener información del último release desde GitHub API
                (string? tag, string? downloadUrl) = await GetLatestReleaseInfoAsync();
                if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(downloadUrl))
                {
                    onProgress?.Invoke("No se pudo obtener información del release.");
                    return false;
                }

                // 2. Comparar con la versión almacenada
                string? savedVersion = Preferences.Get(DbVersionKey, null);
                if (savedVersion == tag)
                {
                    onProgress?.Invoke("La base de datos ya está actualizada.");
                    return true;
                }

                // 3. Descargar y extraer
                onProgress?.Invoke($"Descargando base de datos ({tag})...");
                string zipTemp = Path.Combine(Path.GetTempPath(), "popsmanager_db.zip");

                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                // Necesario para GitHub API y descargas
                client.DefaultRequestHeaders.Add("User-Agent", "POPSManager-Android");

                var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(zipTemp);

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

                onProgress?.Invoke("Extrayendo base de datos...");

                // Extraer en la raíz OPL (sobrescribir si existe)
                if (File.Exists(zipTemp))
                {
                    // Asegurar que la carpeta de destino exista
                    Directory.CreateDirectory(oplRootFolder);
                    ZipFile.ExtractToDirectory(zipTemp, oplRootFolder, true);
                }
                File.Delete(zipTemp);

                // 4. Guardar la versión actual
                Preferences.Set(DbVersionKey, tag);

                onProgress?.Invoke("Base de datos actualizada correctamente.");
                return true;
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

                // La URL directa del asset llamado POPSManager_DB.zip
                var assets = root.GetProperty("assets").EnumerateArray();
                string? downloadUrl = assets
                    .FirstOrDefault(a => a.GetProperty("name").GetString() == "POPSManager_DB.zip")
                    .GetProperty("browser_download_url")
                    .GetString();

                return (tag, downloadUrl);
            }
            catch
            {
                // Fallback: usar la URL estándar de descarga por si la API falla
                return ("unknown", DefaultDownloadUrl);
            }
        }
    }
}