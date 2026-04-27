using System;
using System.Collections.Generic;
using System.Reflection;

namespace POPSManager.Core.Logic
{
    public static class GameDatabase
    {
        private static readonly Dictionary<string, GameEntry> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static bool TryGetEntry(string gameId, out GameEntry? entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(gameId)) return false;
            if (Cache.TryGetValue(gameId, out var cached)) { entry = cached; return true; }

            // Cargar desde recursos embebidos si existen
            var asm = Assembly.GetExecutingAssembly();
            foreach (var res in asm.GetManifestResourceNames())
            {
                if (res.EndsWith("ps1db.json") || res.EndsWith("ps2db.json"))
                {
                    // Placeholder: aquí iría la lógica de carga real
                }
            }

            return false;
        }

        public static string? TryGetCover(string gameId) => null;
    }

    public class GameEntry
    {
        public string GameId { get; set; } = "";
        public string Name { get; set; } = "";
        public string? CoverUrl { get; set; }
        public string[]? CheatFixes { get; set; }
        public string[]? Tags { get; set; }
        public string Publisher { get; set; } = "";
        public int Year { get; set; }
    }
}