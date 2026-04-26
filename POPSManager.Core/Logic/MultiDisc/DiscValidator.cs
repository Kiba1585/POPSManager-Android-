using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace POPSManager.Logic
{
    /// <summary>
    /// Valida la integridad de un conjunto multidisco.
    /// </summary>
    public static class DiscValidator
    {
        public static bool Validate(List<DiscInfo> discs, Action<string> log)
        {
            if (discs == null || discs.Count <= 1)
            {
                log("[MultiDisc] No es multidisco.");
                return false;
            }

            if (discs.Any(d => d.DiscNumber <= 0))
            {
                log("[MultiDisc] ERROR: No se pudo detectar el número de disco.");
                return false;
            }

            var duplicates = discs
                .GroupBy(d => d.DiscNumber)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicates.Count > 0)
            {
                string dupList = string.Join(", ", duplicates.Select(g => $"CD{g.Key}"));
                log($"[MultiDisc] ERROR: Hay discos duplicados → {dupList}");
                return false;
            }

            if (discs.Count > 4)
            {
                log("[MultiDisc] ERROR: POPStarter solo soporta hasta 4 discos.");
                return false;
            }

            var ordered = discs.OrderBy(d => d.DiscNumber).ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                int expected = i + 1;
                if (ordered[i].DiscNumber != expected)
                {
                    log($"[MultiDisc] ERROR: Secuencia incorrecta. Se esperaba CD{expected}, encontrado CD{ordered[i].DiscNumber}.");
                    return false;
                }
            }

            var titles = discs
                .Select(d => NameCleanerBase.CleanTitleOnly(
                    Path.GetFileNameWithoutExtension(d.FileName)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (titles.Count > 1)
            {
                log("[MultiDisc] ERROR: Los discos parecen pertenecer a juegos distintos.");
                log($"[MultiDisc] Títulos detectados: {string.Join(" | ", titles)}");
                return false;
            }

            foreach (var d in discs)
            {
                if (!d.FolderName.StartsWith("CD", StringComparison.OrdinalIgnoreCase))
                {
                    log($"[MultiDisc] ERROR: Carpeta inválida para {d.FileName}. Debe ser CD1, CD2, etc.");
                    return false;
                }

                if (!int.TryParse(d.FolderName.AsSpan(2), out int folderNum) ||
                    folderNum != d.DiscNumber)
                {
                    log($"[MultiDisc] ERROR: Carpeta incorrecta. {d.FileName} está en {d.FolderName} pero debería estar en CD{d.DiscNumber}.");
                    return false;
                }
            }

            if (!discs.Any(d => d.DiscNumber == 1))
            {
                log("[MultiDisc] ERROR: Falta CD1. Un multidisco siempre debe tener CD1.");
                return false;
            }

            var discNumbers = ordered.Select(d => d.DiscNumber).ToList();
            int max = discNumbers.Max();
            for (int i = 1; i <= max; i++)
            {
                if (!discNumbers.Contains(i))
                {
                    log($"[MultiDisc] ERROR: Falta CD{i}. Secuencia incompleta.");
                    return false;
                }
            }

            log("[MultiDisc] Validación multidisco completada correctamente.");
            return true;
        }
    }
}