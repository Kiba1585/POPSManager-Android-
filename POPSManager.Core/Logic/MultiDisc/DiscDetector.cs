using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    /// <summary>
    /// Detecta el número de disco de un archivo VCD/ISO usando múltiples heurísticas.
    /// </summary>
    public static class DiscDetector
    {
        private static readonly Regex DiscRegex =
            new(@"(?:DISC|DISK|CD)[\s\-_]*0?(\d{1,2})|(?:D)(\d{1,2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static int DetectDiscNumber(string path, Action<string> log)
        {
            string name = Path.GetFileNameWithoutExtension(path) ?? string.Empty;

            // 1. NameCleanerBase (nuevo sistema)
            NameCleanerBase.Clean(name, out string? cdTag);
            if (cdTag != null &&
                cdTag.StartsWith("CD", StringComparison.Ordinal) &&
                int.TryParse(cdTag.AsSpan(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int fromTag) &&
                fromTag > 0)
            {
                log($"[MultiDisc] Detectado desde NameCleanerBase → CD{fromTag}");
                return fromTag;
            }

            // 2. Nombre del archivo
            var m = DiscRegex.Match(name);
            if (m.Success)
            {
                int n = Extract(m);
                log($"[MultiDisc] Detectado desde nombre del archivo → CD{n}");
                return n;
            }

            // 3. Nombre de la carpeta
            string? folder = Path.GetFileName(Path.GetDirectoryName(path));
            if (!string.IsNullOrWhiteSpace(folder))
            {
                m = DiscRegex.Match(folder);
                if (m.Success)
                {
                    int n = Extract(m);
                    log($"[MultiDisc] Detectado desde carpeta → CD{n}");
                    return n;
                }
            }

            // 4. CUE avanzado
            string cuePath = Path.ChangeExtension(path, ".cue");
            if (File.Exists(cuePath))
            {
                string cueText = File.ReadAllText(cuePath);
                m = DiscRegex.Match(cueText);
                if (m.Success)
                {
                    int n = Extract(m);
                    log($"[MultiDisc] Detectado desde CUE → CD{n}");
                    return n;
                }
            }

            // 5. SYSTEM.CNF / GameId
            var id = GameIdDetector.DetectGameId(path);
            if (!string.IsNullOrWhiteSpace(id))
            {
                char last = id[^1];
                if (char.IsDigit(last))
                {
                    int n = last - '0';
                    if (n is >= 1 and <= 4)
                    {
                        log($"[MultiDisc] Detectado desde SYSTEM.CNF → CD{n}");
                        return n;
                    }
                }
            }

            // 6. DISCS.TXT existente
            string? folderPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                string discsTxt = Path.Combine(folderPath, "DISCS.TXT");
                if (File.Exists(discsTxt))
                {
                    var lines = File.ReadAllLines(discsTxt);
                    string fileName = Path.GetFileName(path) ?? string.Empty;

                    string? match = lines.FirstOrDefault(l =>
                        l.Contains(fileName, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        int index = Array.IndexOf(lines, match);
                        if (index >= 0)
                        {
                            int n = index + 1;
                            log($"[MultiDisc] Detectado desde DISCS.TXT → CD{n}");
                            return n;
                        }
                    }
                }
            }

            log("[MultiDisc] Aviso: No se pudo detectar el número de disco. Asignando CD1.");
            return 1;
        }

        private static int Extract(Match m)
        {
            if (m.Groups[1].Success &&
                int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int g1) &&
                g1 > 0)
            {
                return g1;
            }

            if (m.Groups[2].Success &&
                int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int g2) &&
                g2 > 0)
            {
                return g2;
            }

            return 1;
        }
    }
}