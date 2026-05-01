using System;
using System.IO;
using System.Text;

namespace POPSManager.Core.Logic
{
    public static class ElfGenerator
    {
        /// <summary>
        /// Genera un ELF para un juego de PS1.
        /// </summary>
        /// <param name="baseElfPath">Ruta del POPSTARTER.ELF original.</param>
        /// <param name="vcdFullPath">Ruta completa del archivo .VCD.</param>
        /// <param name="outputElfPath">Ruta completa donde se creará el nuevo .ELF.</param>
        /// <param name="discNumber">Número de disco (solo se genera para CD1).</param>
        /// <param name="cleanTitle">Nombre limpio que mostrará OPL.</param>
        /// <param name="gameId">Game ID del juego.</param>
        /// <param name="log">Acción para registrar mensajes.</param>
        public static bool GeneratePs1Elf(string baseElfPath, string vcdFullPath, string outputElfPath, int discNumber, string cleanTitle, string gameId, Action<string> log)
        {
            try
            {
                if (discNumber != 1) { log("[ELF] Solo CD1 genera ELF."); return true; }
                if (!File.Exists(baseElfPath)) { log("[ELF] ERROR: POPSTARTER.ELF no encontrado."); return false; }
                if (!File.Exists(vcdFullPath)) { log("[ELF] ERROR: VCD no encontrado."); return false; }

                // Crear carpeta de destino si no existe
                string? outputDir = Path.GetDirectoryName(outputElfPath);
                if (!string.IsNullOrEmpty(outputDir))
                    Directory.CreateDirectory(outputDir);

                // Copiar el ELF base
                File.Copy(baseElfPath, outputElfPath, true);

                // La ruta del VCD es simplemente el nombre del archivo (porque está directamente en POPS)
                string vcdRelativePath = $"mass:/POPS/{Path.GetFileName(vcdFullPath)}";

                using var stream = new FileStream(outputElfPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                using var writer = new BinaryWriter(stream, Encoding.ASCII, true);

                WriteAsciiFixed(writer, 0x2C, NormalizeAscii(gameId), 10);
                WriteAsciiFixed(writer, 0x100, NormalizeAscii(vcdRelativePath), 128);
                WriteAsciiFixed(writer, 0x220, NormalizeAscii(cleanTitle), 48);

                log("[ELF] ELF PS1 generado correctamente.");
                return true;
            }
            catch (Exception ex) { log($"[ELF] ERROR: {ex.Message}"); return false; }
        }

        private static string NormalizeAscii(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var sb = new StringBuilder();
            foreach (char c in value) sb.Append(c <= 127 ? c : '_');
            return sb.ToString().Trim();
        }

        private static void WriteAsciiFixed(BinaryWriter writer, int offset, string value, int maxLength)
        {
            writer.BaseStream.Seek(offset, SeekOrigin.Begin);
            byte[] bytes = Encoding.ASCII.GetBytes(value);
            if (bytes.Length > maxLength) Array.Resize(ref bytes, maxLength);
            writer.Write(bytes);
            if (bytes.Length < maxLength) writer.Write(new byte[maxLength - bytes.Length]);
        }
    }
}