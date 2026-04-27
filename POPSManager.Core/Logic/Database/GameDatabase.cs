using System;
using System.Collections.Generic;
using System.Reflection;

namespace POPSManager.Core.Logic
{
    public static class GameDatabase
    {
        private static readonly Dictionary<string, Models.GameEntry> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static bool TryGetEntry(string gameId, out Models.GameEntry? entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(gameId)) return false;
            if (Cache.TryGetValue(gameId, out var cached)) { entry = cached; return true; }

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
}