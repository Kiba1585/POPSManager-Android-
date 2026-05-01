using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace POPSManager.Android.Services
{
    public static class DatabaseUpdater
    {
        // URL permanente para descargar siempre la última versión de la base de datos
        private const string DefaultDbUrl =
            "https://github.com/Kiba1585/POPSManager.DBGenerator/releases/latest/download/POPSManager_DB.zip";

        /// <summary>
        /// Descarga el ZIP de la base de datos y lo extrae en la raíz OPL.
        /// </summary>
        public static async Task<bool> DownloadAndExtractDatabaseAsync(
            string oplRootFolder,
            Action<string>? onProgress = null,
            string? customUrl = null)
        {
            string url = customUrl ?? DefaultDbUrl;
            string zipTemp = Path.Combine(Path.GetTempPath(), "popsmanager_db.zip");

            try
            {
                onProgress?.Invoke("Descargando base de datos...");
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
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
                if (Directory.Exists(oplRootFolder) && File.Exists(zipTemp))
                {
                    ZipFile.ExtractToDirectory(zipTemp, oplRootFolder, true);
                }
                File.Delete(zipTemp);

                onProgress?.Invoke("Base de datos actualizada correctamente.");
                return true;
            }
            catch (Exception ex)
            {
                onProgress?.Invoke($"Error al descargar la base de datos: {ex.Message}");
                try { if (File.Exists(zipTemp)) File.Delete(zipTemp); } catch { }
                return false;
            }
        }
    }
}