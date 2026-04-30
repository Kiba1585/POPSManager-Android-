using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace POPSManager.Core.Services
{
    public static class MetadataDownloader
    {
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

    public class GameMetadata
    {
        public string Title { get; set; } = "";
        public string Id { get; set; } = "";
        public string Genre { get; set; } = "";
        public string ReleaseDate { get; set; } = "";
        public string Developer { get; set; } = "";
        public string Description { get; set; } = "";
    }
}