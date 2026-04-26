using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace POPSManager.Core.Services;

public static class MultiDiscManager
{
    public static bool IsDisc(string name)
    {
        return name.Contains("Disc 2", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("CD2", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Disk 2", StringComparison.OrdinalIgnoreCase);
    }

    public static List<string> GroupDiscs(List<string> files)
    {
        var groups = new Dictionary<string, List<string>>();
        foreach (var file in files)
        {
            string baseName = GetBaseName(file);
            if (!groups.ContainsKey(baseName))
                groups[baseName] = new List<string>();
            groups[baseName].Add(file);
        }

        return groups.Values.SelectMany(g => g).ToList();
    }

    private static string GetBaseName(string fileName)
    {
        return fileName
            .Replace("Disc 1", "")
            .Replace("Disc 2", "")
            .Replace("CD1", "")
            .Replace("CD2", "")
            .Replace("(Disc 1)", "")
            .Replace("(Disc 2)", "")
            .Trim();
    }
}