using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace POPSManager.Logic
{
    /// <summary>
    /// Base de datos local de juegos PS1/PS2 con soporte para búsqueda por ID.
    /// </summary>
    public static class GameDatabase
    {
        private static readonly Dictionary<string, GameEntry> Cache =
            new(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, GameEntry>? _ps1Db;
        private static Dictionary<string, GameEntry>? _ps2Db;

        static GameDatabase()
        {
            _ps1Db = LoadEmbeddedJson("POPSManager.Data.ps1db.json");
            _ps2Db = LoadEmbeddedJson("POPSManager.Data.ps2db.json");
        }

        private static Dictionary<string, GameEntry>? LoadEmbeddedJson(string resourceName)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using Stream? stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameDatabase] Recurso no encontrado: {resourceName}");
                    return null;
                }

                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                var data = JsonSerializer.Deserialize<Dictionary<string, GameEntry>>(json);
                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GameDatabase] Error cargando {resourceName}: {ex.Message}");
                return null;
            }
        }

        public static bool TryGetEntry(string gameId, out GameEntry? entry)
        {
            entry = null;

            if (string.IsNullOrWhiteSpace(gameId))
                return false;

            if (Cache.TryGetValue(gameId, out var cached))
            {
                entry = cached;
                return true;
            }

            if (_ps1Db != null && _ps1Db.TryGetValue(gameId, out var ps1Entry))
            {
                Cache[gameId] = ps1Entry;
                entry = ps1Entry;
                return true;
            }

            if (_ps2Db != null && _ps2Db.TryGetValue(gameId, out var ps2Entry))
            {
                Cache[gameId] = ps2Entry;
                entry = ps2Entry;
                return true;
            }

            // Online lookup desactivado por defecto
            // entry = TryOnlineLookup(gameId);
            return false;
        }

        public static string? TryGetCover(string gameId)
        {
            return TryGetEntry(gameId, out var entry) ? entry?.CoverUrl : null;
        }

        public static IEnumerable<string>? TryGetFixes(string gameId)
        {
            return TryGetEntry(gameId, out var entry) ? entry?.CheatFixes : null;
        }

        public static GameEntry? TryGetMetadata(string gameId)
        {
            return TryGetEntry(gameId, out var entry) ? entry : null;
        }
    }

    /// <summary>
    /// Entrada de la base de datos de juegos.
    /// </summary>
    public class GameEntry
    {
        public string GameId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Region { get; set; } = "";
        public string Publisher { get; set; } = "";
        public int Year { get; set; }

        public int DiscCount { get; set; }
        public string[]? DiscNames { get; set; }

        public string? CoverUrl { get; set; }

        public string[]? Tags { get; set; }

        public string[]? CheatFixes { get; set; }
        public string[]? GraphicsFixes { get; set; }
        public string[]? VideoFixes { get; set; }
        public string[]? SoundFixes { get; set; }

        public bool HasFmvIssues { get; set; }
        public bool HasTimingIssues { get; set; }
        public bool RequiresPal60 { get; set; }
        public bool RequiresSkipVideos { get; set; }
        public bool RequiresFixCdda { get; set; }
        public bool RequiresFixGraphics { get; set; }
        public bool RequiresFixSound { get; set; }
    }
}