using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace POPSManager.Logic.Covers
{
    /// <summary>
    /// Descarga carátulas desde una URL y las convierte a formato ART (140x200 JPG).
    /// </summary>
    public static class ArtDownloader
    {
        private static readonly HttpClient HttpClient = new();

        /// <summary>
        /// Descarga una imagen JPG, la redimensiona a 140x200 y la guarda como .ART.
        /// </summary>
        /// <param name="gameId">ID del juego (ej: SLES_12345).</param>
        /// <param name="url">URL de la imagen JPG.</param>
        /// <param name="artFolder">Carpeta de destino para el archivo .ART.</param>
        /// <param name="log">Acción para registrar mensajes.</param>
        /// <returns>Ruta del archivo .ART generado, o null si ocurre un error.</returns>
        public static async Task<string?> DownloadArtAsync(
            string gameId,
            string url,
            string artFolder,
            Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                throw new ArgumentException("El ID del juego no puede estar vacío.", nameof(gameId));
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("La URL no puede estar vacía.", nameof(url));
            if (string.IsNullOrWhiteSpace(artFolder))
                throw new ArgumentException("La carpeta de destino no puede estar vacía.", nameof(artFolder));
            if (log == null)
                throw new ArgumentNullException(nameof(log));

            try
            {
                Directory.CreateDirectory(artFolder);

                string tempJpg = Path.Combine(artFolder, $"{gameId}.jpg");
                string artPath = Path.Combine(artFolder, $"{gameId}.ART");

                byte[] bytes = await HttpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                await File.WriteAllBytesAsync(tempJpg, bytes).ConfigureAwait(false);

                ArtResizer.ResizeToArt(tempJpg, artPath);

                if (File.Exists(tempJpg))
                    File.Delete(tempJpg);

                log($"[COVER] ART generado → {artPath}");
                return artPath;
            }
            catch (HttpRequestException ex)
            {
                log($"[COVER] Error de red descargando carátula: {ex.Message}");
                return null;
            }
            catch (IOException ex)
            {
                log($"[COVER] Error de archivo generando ART: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                log($"[COVER] Error inesperado generando ART: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Wrapper síncrono para compatibilidad con código legacy.
        /// </summary>
        public static string? DownloadArt(
            string gameId,
            string url,
            string artFolder,
            Action<string> log)
        {
            return DownloadArtAsync(gameId, url, artFolder, log)
                .GetAwaiter()
                .GetResult();
        }
    }
}