using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace POPSManager.Core.Services
{
    public static class OplArtDownloader
    {
        // URL del volcado mensual de OPLM ART Database (Internet Archive)
        private const string DefaultArtPackUrl = 
            "https://archive.org/download/oplm-art-2023-11/OPLM_ART_2023_11.zip";

        /// <summary>
        /// Descarga el ZIP de covers y metadatos, y lo extrae en la raíz OPL.
        /// </summary>
        public static async Task<bool> DownloadAndExtractAsync(
            string oplRootFolder,
            Action<string>? onProgress = null,
            string? customUrl = null)
        {
            string url = customUrl ?? DefaultArtPackUrl;
            string zipTemp = Path.Combine(Path.GetTempPath(), "oplm_art.zip");
            string extractTo = oplRootFolder;

            try
            {
                onProgress?.Invoke("Conectando al servidor...");
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
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
                        onProgress?.Invoke($"Descargando... {percent}% ({totalRead / 1024 / 1024} MB)");
                    }
                }

                onProgress?.Invoke("Extrayendo archivos...");
                if (Directory.Exists(extractTo))
                {
                    ZipFile.ExtractToDirectory(zipTemp, extractTo, true);
                }

                File.Delete(zipTemp);
                onProgress?.Invoke("Base de datos actualizada correctamente.");
                return true;
            }
            catch (Exception ex)
            {
                onProgress?.Invoke($"Error en la descarga: {ex.Message}");
                try { if (File.Exists(zipTemp)) File.Delete(zipTemp); } catch { }
                return false;
            }
        }
    }
}