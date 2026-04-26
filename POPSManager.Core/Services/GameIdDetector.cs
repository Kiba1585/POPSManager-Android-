using System;
using System.IO;
using System.Text.RegularExpressions;

namespace POPSManager.Core.Services;

public static class GameIdDetector
{
    private static readonly Regex Ps1Regex = new(
        @"(SLUS|SCUS|SLES|SCES|SLPM|SLPS|SCPS)[-_ ]?(\d{3})[._ ]?(\d{2})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Ps2Regex = new(
        @"(SLUS|SCUS|SLES|SCES|SLPM|SLPS|SCPS)[-_ ]?(\d{5})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string DetectGameId(string filePath)
    {
        string name = Path.GetFileNameWithoutExtension(filePath);
        
        var m = Ps1Regex.Match(name);
        if (m.Success)
            return $"{m.Groups[1].Value}_{m.Groups[2].Value}.{m.Groups[3].Value}";

        m = Ps2Regex.Match(name);
        if (m.Success)
            return $"{m.Groups[1].Value}_{m.Groups[2].Value}";

        return "UNKNOWN";
    }

    public static bool IsPalRegion(string gameId)
    {
        if (string.IsNullOrWhiteSpace(gameId)) return false;
        gameId = gameId.ToUpperInvariant();
        return gameId.StartsWith("SLES", StringComparison.Ordinal) ||
               gameId.StartsWith("SCES", StringComparison.Ordinal);
    }
}