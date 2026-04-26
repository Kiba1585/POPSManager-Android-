using POPSManager.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace POPSManager.Logic
{
    public static class CueParser
    {
        public static CueInfo? Parse(string cuePath, Action<string> log)
        {
            if (!File.Exists(cuePath))
            {
                log($"[CUE] Archivo no encontrado: {cuePath}");
                return null;
            }

            var lines = File.ReadAllLines(cuePath);
            var cue = new CueInfo { CuePath = cuePath };

            CueFile? currentFile = null;
            CueTrack? currentTrack = null;

            foreach (var raw in lines)
            {
                string line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = SplitCueLine(line);
                if (parts.Length == 0)
                    continue;

                switch (parts[0].ToUpperInvariant())
                {
                    case "FILE":
                        currentFile = new CueFile
                        {
                            Path = ResolveFilePath(cuePath, parts[1]),
                            Type = parts.Length > 2 ? parts[2] : "BINARY"
                        };
                        cue.Files.Add(currentFile);
                        break;

                    case "TRACK":
                        currentTrack = new CueTrack
                        {
                            Number = int.Parse(parts[1]),
                            Mode = parts[2],
                            File = currentFile
                        };
                        cue.Tracks.Add(currentTrack);
                        break;

                    case "INDEX":
                        if (currentTrack == null)
                            continue;

                        var idx = new CueIndex
                        {
                            Number = int.Parse(parts[1]),
                        };

                        // ← FIX ULTRA‑PRO
                        int m, s, f;
                        ParseTime(parts[2], out m, out s, out f);
                        idx.Minute = m;
                        idx.Second = s;
                        idx.Frame = f;

                        currentTrack.Indexes.Add(idx);
                        break;
                }
            }

            // Validación: TRACK 01 INDEX 01 debe existir
            var track1 = cue.Tracks.FirstOrDefault(t => t.Number == 1);
            if (track1 == null)
            {
                log("[CUE] ERROR: No existe TRACK 01");
                return null;
            }

            var index1 = track1.Indexes.FirstOrDefault(i => i.Number == 1);
            if (index1 == null)
            {
                log("[CUE] ERROR: TRACK 01 no tiene INDEX 01");
                return null;
            }

            // Archivo BIN principal
            cue.BinPath = track1.File?.Path ?? "";

            if (!File.Exists(cue.BinPath))
            {
                log($"[CUE] ERROR: BIN no encontrado: {cue.BinPath}");
                return null;
            }

            log("[CUE] Parseo completado correctamente.");
            return cue;
        }

        // ------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------

        private static string[] SplitCueLine(string line)
        {
            var parts = new List<string>();
            bool inQuotes = false;
            string current = "";

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current);
                        current = "";
                    }
                }
                else
                {
                    current += c;
                }
            }

            if (current.Length > 0)
                parts.Add(current);

            return parts.ToArray();
        }

        private static void ParseTime(string time, out int m, out int s, out int f)
        {
            var parts = time.Split(':');
            m = int.Parse(parts[0], CultureInfo.InvariantCulture);
            s = int.Parse(parts[1], CultureInfo.InvariantCulture);
            f = int.Parse(parts[2], CultureInfo.InvariantCulture);
        }

        private static string ResolveFilePath(string cuePath, string file)
        {
            string dir = Path.GetDirectoryName(cuePath)!;
            return Path.Combine(dir, file);
        }
    }
}