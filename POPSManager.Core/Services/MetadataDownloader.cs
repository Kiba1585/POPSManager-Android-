using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using POPSManager.Core.Logic;
using POPSManager.Core.Logic.Database; // para RedumpClient si existe

namespace POPSManager.Core.Services
{
    public static class MetadataDownloader
    {
        /// <summary>
        /// Descarga metadatos del juego y los guarda en un archivo .meta en la carpeta CFG.
        /// Si no hay fuente disponible, deja un placeholder.
        /// </summary>
        public static async Task DownloadMetadataAsync(string gameId, string gameName, string cfgFolder, Action<string> log)
        {
            try
            {
                Directory.CreateDirectory(cfgFolder);
                string metaPath = Path.Combine(cfgFolder, $"{gameId}.meta");

                // Intentar obtener metadatos de Redump/GamesDB
                var metadata = await TryFetchMetadataAsync(gameId, gameName);
                if (metadata == null)
                {
                    // Placeholder con el nombre local
                    metadata = new GameMetadata
                    {
                        Title = gameName,
                        Id = gameId,
                        Genre = "Unknown",
                        ReleaseDate = "",
                        Developer = "",
                        Description = ""
                    };
                    log($"[META] No hay fuente externa, usando placeholder para {gameId}");
                }

                var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(metaPath, json);
                log($"[META] Metadatos guardados → {metaPath}");
            }
            catch (Exception ex)
            {
                log($"[META] Error: {ex.Message}");
            }
        }

        private static async Task<GameMetadata?> TryFetchMetadataAsync(string gameId, string gameName)
        {
            // Aquí puedes usar RedumpClient o GameFaqsClient si ya están implementados
            // Ejemplo: return await RedumpClient.GetGameMetadataAsync(gameId);
            // Mientras tanto, devolvemos null para que se ponga el placeholder.
            return await Task.FromResult<GameMetadata?>(null);
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
        // ... otros campos si quieres
    }
}