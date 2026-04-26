using SkiaSharp;
using System;
using System.IO;

namespace POPSManager.Core.Logic.Covers;

public static class ArtResizer
{
    public static bool ResizeToArt(string inputPath, string outputArtPath, Action<string>? log = null)
    {
        try
        {
            using var input = File.OpenRead(inputPath);
            using var original = SKBitmap.Decode(input);
            
            if (original == null)
            {
                log?.Invoke("[ART] Error: No se pudo decodificar la imagen.");
                return false;
            }

            using var resized = original.Resize(new SKImageInfo(140, 200), SKFilterQuality.High);
            if (resized == null)
            {
                log?.Invoke("[ART] Error: No se pudo redimensionar la imagen.");
                return false;
            }

            using var image = SKImage.FromBitmap(resized);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
            
            using var output = File.OpenWrite(outputArtPath);
            data.SaveTo(output);

            return true;
        }
        catch (Exception ex)
        {
            log?.Invoke($"[ART] Error al redimensionar: {ex.Message}");
            return false;
        }
    }
}