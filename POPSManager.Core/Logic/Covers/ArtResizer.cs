using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace POPSManager.Logic.Covers
{
    /// <summary>
    /// Redimensiona imágenes al formato requerido por OPL (140x200 JPG renombrado a .ART).
    /// </summary>
    public static class ArtResizer
    {
        /// <summary>
        /// Redimensiona una imagen a 140x200 píxeles y la guarda como .ART.
        /// </summary>
        /// <param name="inputPath">Ruta de la imagen original (JPG).</param>
        /// <param name="outputArtPath">Ruta de destino del archivo .ART.</param>
        /// <param name="log">Acción opcional para registrar mensajes.</param>
        /// <returns>True si la imagen final tiene exactamente 140x200; False en caso contrario.</returns>
        public static bool ResizeToArt(string inputPath, string outputArtPath, Action<string>? log = null)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                throw new ArgumentException("La ruta de entrada no puede estar vacía.", nameof(inputPath));
            if (string.IsNullOrWhiteSpace(outputArtPath))
                throw new ArgumentException("La ruta de salida no puede estar vacía.", nameof(outputArtPath));
            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"No se encontró el archivo: {inputPath}");

            try
            {
                // Cargar imagen original
                var original = new BitmapImage();
                original.BeginInit();
                original.UriSource = new Uri(inputPath, UriKind.Absolute);
                original.CacheOption = BitmapCacheOption.OnLoad;
                original.EndInit();

                // Verificar dimensiones originales
                if (original.PixelWidth == 140 && original.PixelHeight == 200)
                {
                    // Ya tiene el tamaño exacto, copiar sin redimensionar
                    File.Copy(inputPath, outputArtPath, true);
                    log?.Invoke($"[ART] Imagen ya tiene tamaño 140x200, copiada directamente.");
                    return true;
                }

                // Redimensionar a 140x200
                var scale = new ScaleTransform(
                    scaleX: 140.0 / original.PixelWidth,
                    scaleY: 200.0 / original.PixelHeight
                );

                var transformed = new TransformedBitmap(original, scale);

                // Codificar como JPG (extensión .ART)
                var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
                encoder.Frames.Add(BitmapFrame.Create(transformed));

                using var fs = new FileStream(outputArtPath, FileMode.Create, FileAccess.Write);
                encoder.Save(fs);

                // Verificar que el resultado tenga el tamaño exacto
                var result = new BitmapImage();
                result.BeginInit();
                result.UriSource = new Uri(outputArtPath, UriKind.Absolute);
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.EndInit();

                if (result.PixelWidth == 140 && result.PixelHeight == 200)
                {
                    log?.Invoke($"[ART] Imagen redimensionada correctamente a 140x200.");
                    return true;
                }
                else
                {
                    log?.Invoke($"[ART] ADVERTENCIA: La imagen redimensionada tiene {result.PixelWidth}x{result.PixelHeight}, no 140x200.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"[ART] Error al redimensionar imagen: {ex.Message}");
                return false;
            }
        }
    }
}