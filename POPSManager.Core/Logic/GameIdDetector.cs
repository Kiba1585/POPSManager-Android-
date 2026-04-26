using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class GameIdDetector
    {
        private static readonly Regex Ps1Regex =
            new(@"(SLUS|SCUS|SLES|SCES|SLPM|SLPS|SCPS)[-_ ]?(\d{3})[._ ]?(\d{2})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex Ps2Regex =
            new(@"(SLUS|SCUS|SLES|SCES|SLPM|SLPS|SCPS)[-_ ]?(\d{5})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const int SectorSize = 2048;

        public static string? DetectGameId(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);

                int rootLba = GetRootDirectoryLba(fs);

                if (rootLba > 0)
                {
                    var sys = FindFile(fs, rootLba, "SYSTEM.CNF");
                    if (sys.lba > 0)
                    {
                        var data = ReadFileFromIso(fs, sys.lba, sys.size);
                        var id = ExtractPs2Id(data) ?? ExtractPs1Id(data);
                        if (id != null)
                            return Normalize(id);
                    }
                }

                fs.Seek(0, SeekOrigin.Begin);
                var deep = ScanForId(fs, 2 * 1024 * 1024);
                if (deep != null)
                    return Normalize(deep);
            }
            catch
            {
                // Ignorar errores
            }

            var nameId = DetectFromName(Path.GetFileName(path));
            return Normalize(nameId);
        }

        private static int GetRootDirectoryLba(FileStream fs)
        {
            try
            {
                var pvd = ReadSector(fs, 16);
                return BitConverter.ToInt32(pvd, 156 + 2);
            }
            catch { return 0; }
        }

        private static byte[] ReadSector(FileStream fs, int lba, int count = 1)
        {
            byte[] buffer = new byte[SectorSize * count];
            fs.Seek(lba * SectorSize, SeekOrigin.Begin);
            fs.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        private static (int lba, int size) FindFile(FileStream fs, int rootLba, string target)
        {
            try
            {
                var sector = ReadSector(fs, rootLba);
                int pos = 0;

                while (pos < sector.Length)
                {
                    int len = sector[pos];
                    if (len == 0)
                        break;

                    int nameLen = sector[pos + 32];
                    string name = Encoding.ASCII.GetString(sector, pos + 33, nameLen)
                        .TrimEnd(';', '1');

                    if (name.Equals(target, StringComparison.OrdinalIgnoreCase))
                    {
                        int lba = BitConverter.ToInt32(sector, pos + 2);
                        int size = BitConverter.ToInt32(sector, pos + 10);
                        return (lba, size);
                    }

                    pos += len;
                }
            }
            catch { }

            return (0, 0);
        }

        private static byte[] ReadFileFromIso(FileStream fs, int lba, int size)
        {
            int sectors = (size + SectorSize - 1) / SectorSize;
            return ReadSector(fs, lba, sectors);
        }

        private static string? ExtractPs1Id(byte[] data)
        {
            string text = Encoding.ASCII.GetString(data);
            var m = Ps1Regex.Match(text);
            if (m.Success)
                return $"{m.Groups[1].Value}_{m.Groups[2].Value}.{m.Groups[3].Value}";
            return null;
        }

        private static string? ExtractPs2Id(byte[] data)
        {
            string text = Encoding.ASCII.GetString(data);
            var m = Ps2Regex.Match(text);
            if (m.Success)
                return $"{m.Groups[1].Value}_{m.Groups[2].Value}";
            return null;
        }

        private static string? ScanForId(FileStream fs, int bytes)
        {
            int toRead = (int)Math.Min(bytes, fs.Length);
            byte[] buffer = new byte[toRead];
            fs.Read(buffer, 0, toRead);

            string text = Encoding.ASCII.GetString(buffer);

            var m1 = Ps1Regex.Match(text);
            if (m1.Success)
                return $"{m1.Groups[1].Value}_{m1.Groups[2].Value}.{m1.Groups[3].Value}";

            var m2 = Ps2Regex.Match(text);
            if (m2.Success)
                return $"{m2.Groups[1].Value}_{m2.Groups[2].Value}";

            return null;
        }

        public static string DetectFromName(string name)
        {
            name = name.ToUpperInvariant();

            var m1 = Ps1Regex.Match(name);
            if (m1.Success)
                return $"{m1.Groups[1].Value}_{m1.Groups[2].Value}.{m1.Groups[3].Value}";

            var m2 = Ps2Regex.Match(name);
            if (m2.Success)
                return $"{m2.Groups[1].Value}_{m2.Groups[2].Value}";

            return "";
        }

        private static string Normalize(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return "";

            id = id.ToUpperInvariant()
                   .Replace("-", "_")
                   .Replace(" ", "_")
                   .Replace(".", "_");

            return id;
        }

        public static bool IsPalRegion(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return false;

            gameId = gameId.ToUpperInvariant();

            return gameId.StartsWith("SLES", StringComparison.Ordinal) ||
                   gameId.StartsWith("SCES", StringComparison.Ordinal);
        }

        /// <summary>
        /// Determina si un juego requiere el fix PAL60 basado en su GameID.
        /// </summary>
        public static bool RequiresPal60(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return false;

            string[] pal60Ids =
            {
                "SLES_01514", // Silent Hill
                "SLES_01370", // Metal Gear Solid
                "SLES_02080", // Final Fantasy VIII
                "SLES_02965", // Final Fantasy IX
                "SCES_01237", // Tekken 3
                "SCES_02105", // Crash Team Racing
                "SCES_02104", // Spyro 2
                "SCES_02835"  // Spyro 3
            };

            foreach (var id in pal60Ids)
            {
                if (gameId.StartsWith(id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}