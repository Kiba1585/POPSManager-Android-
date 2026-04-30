using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace POPSManager.Core.Services
{
    public static class OplMetadataReader
    {
        /// <summary>
        /// Lee un archivo .cfg de OPL y devuelve los pares clave=valor.
        /// </summary>
        public static async Task<Dictionary<string, string>> ReadCfgFileAsync(string cfgPath)
        {
            var result = new Dictionary<string, string>();
            if (!File.Exists(cfgPath))
                return result;

            try
            {
                string[] lines = await File.ReadAllLinesAsync(cfgPath);
                foreach (string line in lines)
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    result[key] = value;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error leyendo CFG: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Obtiene los metadatos más relevantes para mostrar en la UI.
        /// </summary>
        public static async Task<GameMetadata> GetCfgMetadataAsync(string gameId, string cfgFolder)
        {
            string cfgPath = Path.Combine(cfgFolder, $"{gameId}.cfg");
            var data = await ReadCfgFileAsync(cfgPath);

            return new GameMetadata
            {
                Id = gameId,
                Title = data.TryGetValue("Title", out var title) ? title : gameId,
                Genre = data.TryGetValue("Genre", out var genre) ? genre : "Unknown",
                ReleaseDate = data.TryGetValue("Release", out var release) ? release : "",
                Developer = data.TryGetValue("Developer", out var dev) ? dev : "",
                Description = data.TryGetValue("Description", out var desc) ? desc : ""
            };
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