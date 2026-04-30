using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using POPSManager.Core.Models;

namespace POPSManager.Core.Logic
{
    public static class GameDatabase
    {
        private static readonly Dictionary<string, GameEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            LoadEmbeddedJson("POPSManager.Core.Data.ps1db.json");
            LoadEmbeddedJson("POPSManager.Core.Data.ps2db.json");
        }

        private static void LoadEmbeddedJson(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null) return;

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var dict = JsonSerializer.Deserialize<Dictionary<string, GameEntry>>(json);
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    if (!_entries.ContainsKey(kvp.Key))
                        _entries[kvp.Key] = kvp.Value;
                }
            }
        }

        public static bool TryGetEntry(string gameId, out GameEntry? entry)
        {
            Initialize();
            return _entries.TryGetValue(gameId, out entry);
        }

        public static string? TryGetCoverUrl(string gameId)
        {
            if (TryGetEntry(gameId, out var entry) && !string.IsNullOrWhiteSpace(entry?.CoverUrl))
                return entry.CoverUrl;
            return null;
        }
    }
}