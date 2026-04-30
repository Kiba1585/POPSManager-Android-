using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace POPSManager.Core.Logic
{
    public static class CheatGenerator
    {
        /// <summary>
        /// Genera CHEAT.TXT con las líneas base + líneas adicionales (sin duplicar).
        /// </summary>
        public static void GenerateCheatTxt(string gameId, string popsDiscFolder, IEnumerable<string>? extraLines, Action<string> log)
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

                // Añadir líneas extra del usuario (sin repetir)
                if (extraLines != null)
                {
                    foreach (var line in extraLines)
                        if (!lines.Contains(line, StringComparer.OrdinalIgnoreCase))
                            lines.Add(line);
                }

                File.WriteAllLines(cheatPath, lines);
                log($"[PS1] CHEAT.TXT generado → {cheatPath}");
            }
            catch (Exception ex) { log($"[PS1] Error generando CHEAT.TXT: {ex.Message}"); }
        }

        // Sobrecarga sin extraLines para mantener compatibilidad
        public static void GenerateCheatTxt(string gameId, string popsDiscFolder, Action<string> log)
        {
            GenerateCheatTxt(gameId, popsDiscFolder, null, log);
        }

        private static bool IsMultiDiscFolder(string folder)
        {
            string? parent = Path.GetDirectoryName(folder);
            return parent != null && Directory.GetFiles(parent).Any(f => f.EndsWith("DISCS.TXT", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsDisc1Folder(string folder) =>
            (Path.GetFileName(folder) ?? "").ToUpperInvariant() switch
            {
                string n when n.Contains("CD1") || n.Contains("(CD1)") || n.Contains("DISC1") => true,
                _ => false
            };

        private static void ApplyGameSpecificFixes(string gameId, List<string> lines, Action<string> log)
        {
            // … (el código original tal cual) …
        }

        private static void ApplyEngineFixes(string gameId, List<string> lines, Action<string> log)
        {
            // … (el código original tal cual) …
        }

        private static void ApplyHeuristicFixes(string gameId, List<string> lines, Action<string> log)
        {
            // … (el código original tal cual) …
        }

        private static void ApplyDatabaseFixes(string gameId, List<string> lines, Action<string> log)
        {
            // … (el código original tal cual) …
        }
    }
}