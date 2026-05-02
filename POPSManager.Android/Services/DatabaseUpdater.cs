using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using POPSManager.Core.Models;

namespace POPSManager.Core.Logic
{
    public static class GameDatabase
    {
        private static Dictionary<string, GameEntry>? _entries;
        private static readonly object _lock = new();

        public static void Initialize(string dataFolder)
        {
            if (_entries != null) return;
            lock (_lock)
            {
                if (_entries != null) return;
                _entries = new Dictionary<string, GameEntry>(StringComparer.OrdinalIgnoreCase);

                LoadJson(Path.Combine(dataFolder, "ps1db.json"));
                LoadJson(Path.Combine(dataFolder, "ps2db.json"));
            }
        }

        private static void LoadJson(string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                string json = File.ReadAllText(path);
                var dict = JsonSerializer.Deserialize<Dictionary<string, GameEntry>>(json);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                        _entries!.TryAdd(kvp.Key, kvp.Value);
                }
            }
            catch { }
        }

        public static bool TryGetEntry(string gameId, out GameEntry? entry)
        {
            _entries ??= new Dictionary<string, GameEntry>();
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