using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace POPSManager.Core.Logic.MultiDisc
{
    public static class DiscDetector
    {
        private static readonly Regex DiscRegex = new(@"(?:DISC|DISK|CD)[\s\-_]*0?(\d{1,2})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static int DetectDiscNumber(string path, Action<string> log)
        {
            string name = Path.GetFileNameWithoutExtension(path) ?? "";
            var m = DiscRegex.Match(name);
            if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0)
            {
                log($"[MultiDisc] Número de disco: CD{n}");
                return n;
            }

            string? folder = Path.GetFileName(Path.GetDirectoryName(path));
            if (!string.IsNullOrWhiteSpace(folder))
            {
                m = DiscRegex.Match(folder);
                if (m.Success && int.TryParse(m.Groups[1].Value, out n) && n > 0)
                {
                    log($"[MultiDisc] Detectado por carpeta: CD{n}");
                    return n;
                }
            }

            log("[MultiDisc] Asignado CD1 por defecto.");
            return 1;
        }
    }
}