using System;
using System.IO;

namespace POPSManager.Logic
{
    public static class NameFormatter
    {
        // ============================================================
        //  PS1 — Nombre final del VCD
        // ============================================================
        public static string BuildPs1VcdName(string discPath, int discNumber, string gameId, string cleanTitle)
        {
            string ext = ".VCD";

            string title = cleanTitle;

            if (discNumber > 1)
                title += $" (Disc {discNumber})";

            return $"{gameId} - {title}{ext}";
        }

        // ============================================================
        //  PS2 — Nombre final del ISO
        // ============================================================
        public static string BuildPs2IsoName(string isoPath, string gameId, string cleanTitle)
        {
            string ext = ".iso";
            return $"{gameId} - {cleanTitle}{ext}";
        }

        // ============================================================
        //  Carpeta POPS (PS1)
        // ============================================================
        public static string BuildPopsFolderName(string gameId, string cleanTitle)
        {
            return $"{gameId} - {cleanTitle}";
        }

        // ============================================================
        //  Nombre del ELF (PS1)
        // ============================================================
        public static string BuildElfName(string gameId, string cleanTitle)
        {
            return $"{gameId} - {cleanTitle}.ELF.NTSC";
        }
    }
}