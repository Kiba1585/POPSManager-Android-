using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using POPSManager.Logic.Automation;

namespace POPSManager.Logic
{
    /// <summary>
    /// Gestor de juegos multidisco: detección, validación y generación de DISCS.TXT.
    /// </summary>
    public static class MultiDiscManager
    {
        private static readonly Regex SimpleDiscRegex =
            new(@"(?:disc|disk|cd)[\s\-_]*0?(\d{1,2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static int ExtractDiscNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 1;

            var match = SimpleDiscRegex.Match(input);
            if (match.Success &&
                int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) &&
                n > 0)
            {
                return n;
            }

            return 1;
        }

        // ============================================================
        //  GENERAR DISCS.TXT (ASYNC + AUTOMATIZACIÓN)
        // ============================================================
        public static async Task GenerateDiscsTxtAsync(
            string popsFolder,
            string gameId,
            List<string> discPaths,
            Action<string> log,
            AutomationEngine? auto = null)
        {
            if (discPaths == null || discPaths.Count == 0)
            {
                log("[MultiDisc] No hay discos para generar DISCS.TXT.");
                return;
            }

            // AUTOMATIZACIÓN
            if (auto != null && !auto.ShouldHandleMultiDisc())
            {
                log("[MultiDisc] Automatización: multidisco desactivado. No se genera DISCS.TXT.");
                return;
            }

            log("[MultiDisc] Iniciando validación multidisco…");

            var discs = discPaths
                .Select(path => new DiscInfo
                {
                    Path = path,
                    DiscNumber = DiscDetector.DetectDiscNumber(path, log),
                    GameId = GameIdDetector.DetectGameId(path) ?? string.Empty,
                    FolderName = Path.GetFileName(Path.GetDirectoryName(path)) ?? string.Empty,
                    FileName = Path.GetFileName(path) ?? string.Empty
                })
                .ToList();

            if (!DiscValidator.Validate(discs, log))
            {
                log("[MultiDisc] ERROR: Validación multidisco falló. No se generará DISCS.TXT.");
                return;
            }

            discs = discs.OrderBy(d => d.DiscNumber).ToList();

            var lines = discs
                .Select(d => $"mass:/POPS/{d.FolderName}/{d.FileName}")
                .ToList();

            // Escritura async para cada carpeta CDX
            foreach (var d in discs)
            {
                string folder = Path.GetDirectoryName(d.Path)!;
                string discsTxtPath = Path.Combine(folder, "DISCS.TXT");

                await File.WriteAllLinesAsync(discsTxtPath, lines)
                    .ConfigureAwait(false);

                log($"[MultiDisc] DISCS.TXT generado → {discsTxtPath}");
            }

            log("[MultiDisc] DISCS.TXT generado correctamente para todos los discos.");
        }

        // ============================================================
        //  WRAPPER SÍNCRONO (COMPATIBILIDAD)
        // ============================================================
        public static void GenerateDiscsTxt(
            string popsFolder,
            string gameId,
            List<string> discPaths,
            Action<string> log,
            AutomationEngine? auto = null)
        {
            GenerateDiscsTxtAsync(popsFolder, gameId, discPaths, log, auto)
                .GetAwaiter()
                .GetResult();
        }
    }
}