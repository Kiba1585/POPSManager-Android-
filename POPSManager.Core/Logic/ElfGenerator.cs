using System;
using System.IO;
using System.Text;

namespace POPSManager.Core.Logic
{
    public static class ElfGenerator
    {
        public static bool GeneratePs1Elf(string baseElfPath, string vcdFullPath, string appsFolder, int discNumber, string cleanTitle, string gameId, Action<string> log)
        {
            try
            {
                if (discNumber != 1) { log("[ELF] Solo CD1 genera ELF."); return true; }
                if (!File.Exists(baseElfPath)) { log("[ELF] ERROR: POPSTARTER.ELF no encontrado."); return false; }
                if (!File.Exists(vcdFullPath)) { log("[ELF] ERROR: VCD no encontrado."); return false; }

                Directory.CreateDirectory(appsFolder);
                string elfFileName = $"{gameId} - {cleanTitle}.ELF.NTSC";
                string outputElf = Path.Combine(appsFolder, elfFileName);
                string vcdRelativePath = $"mass:/POPS/{Path.GetFileName(Path.GetDirectoryName(vcdFullPath))}/{Path.GetFileName(vcdFullPath)}";

                File.Copy(baseElfPath, outputElf, true);
                using var stream = new FileStream(outputElf, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
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