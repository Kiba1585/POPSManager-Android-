namespace POPSManager.Core.Services;

public static class GameIdDetector
{
    public static string Detect(string filePath)
    {
        var file = Path.GetFileNameWithoutExtension(filePath);

        // Ejemplo simple
        if (file.Contains("SLUS", StringComparison.OrdinalIgnoreCase))
            return "SLUS";

        if (file.Contains("SCUS", StringComparison.OrdinalIgnoreCase))
            return "SCUS";

        return "UNKNOWN";
    }
}
