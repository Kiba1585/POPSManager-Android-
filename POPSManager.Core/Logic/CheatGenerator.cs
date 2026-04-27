using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace POPSManager.Core.Logic
{
    public static class CheatGenerator
    {
        public static void GenerateCheatTxt(string gameId, string popsDiscFolder, Action<string> log)
        {
            try
            {
                if (!GameIdValidator.IsPs1(gameId)) { log("[PS1] No se genera CHEAT.TXT para PS2."); return; }
                if (!GameIdDetector.IsPalRegion(gameId)) { log("[PS1] No se genera CHEAT.TXT para juegos NTSC."); return; }
                if (IsMultiDiscFolder(popsDiscFolder) && !IsDisc1Folder(popsDiscFolder)) { log("[PS1] Multidisco. CHEAT.TXT solo en CD1."); return; }

                string cheatPath = Path.Combine(popsDiscFolder, "CHEAT.TXT");
                var lines = new List<string> { "VMODE=NTSC", "BORDER=OFF", "CENTER=ON" };
                if (GameIdDetector.RequiresPal60(gameId)) lines.Add("FORCEVIDEO=1");

                ApplyGameSpecificFixes(gameId, lines, log);
                ApplyEngineFixes(gameId, lines, log);
                ApplyHeuristicFixes(gameId, lines, log);
                ApplyDatabaseFixes(gameId, lines, log);

                File.WriteAllLines(cheatPath, lines);
                log($"[PS1] CHEAT.TXT generado → {cheatPath}");
            }
            catch (Exception ex) { log($"[PS1] Error generando CHEAT.TXT: {ex.Message}"); }
        }

        private static bool IsMultiDiscFolder(string folder)
        {
            string? parent = Path.GetDirectoryName(folder);
            return parent != null && Directory.GetFiles(parent).Any(f => f.EndsWith("DISCS.TXT", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsDisc1Folder(string folder) => (Path.GetFileName(folder) ?? "").ToUpperInvariant() is string n && (n.Contains("CD1") || n.Contains("(CD1)") || n.Contains("DISC1"));

        private static void ApplyGameSpecificFixes(string gameId, List<string> lines, Action<string> log)
        {
            var fixes = new Dictionary<string, Action>
            {
                {"SLES00972", () => { lines.Add("FORCEVIDEO=1"); lines.Add("SKIPVIDEOS=ON"); } },
                {"SLES10972", () => { lines.Add("FORCEVIDEO=1"); lines.Add("SKIPVIDEOS=ON"); } },
                {"SLES02529", () => { lines.Add("FORCEVIDEO=1"); lines.Add("SKIPVIDEOS=ON"); } },
                {"SLES01514", () => lines.Add("FORCEVIDEO=1") },
                {"SLES01370", () => lines.Add("FORCEVIDEO=1") },
                {"SLES11370", () => lines.Add("FORCEVIDEO=1") },
                {"SLES02080", () => lines.Add("FORCEVIDEO=1") },
                {"SLES12080", () => lines.Add("FORCEVIDEO=1") },
                {"SLES02965", () => lines.Add("FORCEVIDEO=1") },
                {"SLES12965", () => lines.Add("FORCEVIDEO=1") },
                {"SLES12380", () => { lines.Add("FORCEVIDEO=1"); lines.Add("FIXGRAPHICS=ON"); } },
                {"SCES01237", () => lines.Add("FORCEVIDEO=1") },
                {"SCES02105", () => lines.Add("FORCEVIDEO=1") },
                {"SCES02104", () => lines.Add("FORCEVIDEO=1") },
                {"SCES02835", () => lines.Add("FORCEVIDEO=1") }
            };

            string id = gameId.ToUpperInvariant();
            foreach (var kv in fixes)
                if (id.StartsWith(kv.Key, StringComparison.Ordinal)) { kv.Value(); log($"[PS1] Fix aplicado: {kv.Key}"); }
        }

        private static void ApplyEngineFixes(string gameId, List<string> lines, Action<string> log)
        {
            string id = gameId.ToUpperInvariant();
            if (id.StartsWith("SCES00", StringComparison.Ordinal) || id.StartsWith("SCUS94", StringComparison.Ordinal)) { lines.Add("FIXSOUND=ON"); log("[PS1] Fix engine: Crash"); }
            if (id.StartsWith("SCES02", StringComparison.Ordinal) || id.StartsWith("SCUS94", StringComparison.Ordinal)) { lines.Add("FIXGRAPHICS=ON"); log("[PS1] Fix engine: Spyro"); }
            if (id.StartsWith("SLES02", StringComparison.Ordinal) || id.StartsWith("SCES02", StringComparison.Ordinal)) { lines.Add("FIXCDDA=ON"); log("[PS1] Fix engine: Final Fantasy"); }
        }

        private static void ApplyHeuristicFixes(string gameId, List<string> lines, Action<string> log)
        {
            if (gameId.ToUpperInvariant() is string id && (id.EndsWith("80", StringComparison.Ordinal) || id.EndsWith("65", StringComparison.Ordinal))) { lines.Add("FORCEVIDEO=1"); log("[PS1] Heurística: timings sensibles"); }
        }

        private static void ApplyDatabaseFixes(string gameId, List<string> lines, Action<string> log)
        {
            if (GameDatabase.TryGetEntry(gameId, out var entry) && entry?.CheatFixes != null)
                foreach (var fix in entry.CheatFixes) if (!string.IsNullOrWhiteSpace(fix)) { lines.Add(fix); log($"[PS1] Fix DB: {fix}"); }
        }
    }
}