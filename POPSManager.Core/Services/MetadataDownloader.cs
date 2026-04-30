using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace POPSManager.Core.Services
{
    public static class MetadataDownloader
    {
        /// <summary>
        /// Genera un archivo .meta con metadatos placeholder (usado como fallback).
        /// </summary>
        public static async Task DownloadMetadataAsync(string gameId, string gameName, string cfgFolder, Action<string> log)
        {
            try
            {
                Directory.CreateDirectory(cfgFolder);
                string metaPath = Path.Combine(cfgFolder, $"{gameId}.meta");

                var metadata = new GameMetadata
                {
                    Title = gameName,
                    Id = gameId,
                    Genre = "Unknown",
                    ReleaseDate = "",
                    Developer = "",
                    Description = ""
                };

                var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(metaPath, json);
                log($"[META] Metadatos guardados → {metaPath}");
            }
            catch (Exception ex)
            {
                log($"[META] Error: {ex.Message}");
            }
        }
    }
}