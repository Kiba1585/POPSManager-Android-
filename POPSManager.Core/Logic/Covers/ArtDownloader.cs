using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace POPSManager.Core.Logic.Covers
{
    public static class ArtDownloader
    {
        private static readonly HttpClient HttpClient = new();

        public static async Task<string?> DownloadArtAsync(string gameId, string url, string artFolder, Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(gameId)) throw new ArgumentException("gameId");
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("url");
            if (string.IsNullOrWhiteSpace(artFolder)) throw new ArgumentException("artFolder");
            if (log == null) throw new ArgumentNullException(nameof(log));

            try
            {
                Directory.CreateDirectory(artFolder);
                string tempJpg = Path.Combine(artFolder, $"{gameId}.jpg");
                string artPath = Path.Combine(artFolder, $"{gameId}.ART");
                byte[] bytes = await HttpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                await File.WriteAllBytesAsync(tempJpg, bytes).ConfigureAwait(false);
                ArtResizer.ResizeToArt(tempJpg, artPath, log);
                if (File.Exists(tempJpg)) File.Delete(tempJpg);
                log($"[COVER] ART generado → {artPath}");
                return artPath;
            }
            catch (Exception ex)
            {
                log($"[COVER] Error generando ART: {ex.Message}");
                return null;
            }
        }

        public static string? DownloadArt(string gameId, string url, string artFolder, Action<string> log)
        {
            return DownloadArtAsync(gameId, url, artFolder, log).GetAwaiter().GetResult();
        }
    }
}