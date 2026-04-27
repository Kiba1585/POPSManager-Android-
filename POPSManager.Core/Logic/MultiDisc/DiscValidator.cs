using System;
using System.Collections.Generic;
using System.Linq;

namespace POPSManager.Core.Logic.MultiDisc
{
    public static class DiscValidator
    {
        public static bool Validate(List<DiscInfo> discs, Action<string> log)
        {
            if (discs == null || discs.Count <= 1) { log("[MultiDisc] No es multidisco."); return false; }
            if (discs.Any(d => d.DiscNumber <= 0)) { log("[MultiDisc] ERROR: Número de disco inválido."); return false; }

            var ordered = discs.OrderBy(d => d.DiscNumber).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].DiscNumber != i + 1)
                {
                    log($"[MultiDisc] ERROR: Secuencia incorrecta.");
                    return false;
                }
            }

            log("[MultiDisc] Validación correcta.");
            return true;
        }
    }
}