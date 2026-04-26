using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace POPSManager.Logic.Inspectors
{
    /// <summary>
    /// Inspector para archivos ISO de PS1/PS2.
    /// Extrae información como Game ID, región, PVD y archivos internos.
    /// </summary>
    public static class IsoInspector
    {
        private const int SectorSize = 2048;

        // PS1: SCES_123.45  /  PS2: SLUS_12345
        private static readonly Regex IdRegex =
            new(@"(SLUS|SCUS|SLES|SCES|SLPM|SLPS|SCPS)[-_ ]?(\d{3,5})(?:[._ ]?(\d{2}))?",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Inspecciona un archivo ISO y devuelve la información extraída.
        /// </summary>
        /// <param name="isoPath">Ruta completa al archivo ISO.</param>
        /// <returns>Objeto <see cref="IsoInfo"/> con los datos del ISO.</returns>
        public static IsoInfo Inspect(string isoPath)
        {
            if (!File.Exists(isoPath))
                throw new FileNotFoundException($"Archivo no encontrado: {isoPath}");

            using var fs = new FileStream(isoPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var info = new IsoInfo
            {
                Path = isoPath,
                SizeBytes = fs.Length,
                Pvd = ReadPvd(fs),
                Files = ReadRootDirectory(fs)
            };

            info.SystemCnf = ReadFile(fs, info.Files, "SYSTEM.CNF");
            info.IoprpImg = ReadFile(fs, info.Files, "IOPRP.IMG");

            info.GameId = ExtractId(info.SystemCnf) ?? ExtractId(info.IoprpImg);
            info.Region = DetectRegion(info.GameId);

            return info;
        }

        private static string DetectRegion(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return "Unknown";

            if (id.StartsWith("SLUS", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("SCUS", StringComparison.OrdinalIgnoreCase))
                return "NTSC-U";

            if (id.StartsWith("SLES", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("SCES", StringComparison.OrdinalIgnoreCase))
                return "PAL";

            if (id.StartsWith("SLPM", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("SLPS", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("SCPS", StringComparison.OrdinalIgnoreCase))
                return "NTSC-J";

            return "Unknown";
        }

        private static string? ExtractId(byte[]? data)
        {
            if (data == null || data.Length == 0)
                return null;

            string text = Encoding.ASCII.GetString(data);
            var m = IdRegex.Match(text);
            if (!m.Success)
                return null;

            string prefix = m.Groups[1].Value;
            string numbers = m.Groups[2].Value;
            string suffix = m.Groups[3].Success ? "." + m.Groups[3].Value : "";

            return $"{prefix}_{numbers}{suffix}";
        }

        private static PvdInfo ReadPvd(FileStream fs)
        {
            try
            {
                fs.Seek(16 * SectorSize, SeekOrigin.Begin);

                byte[] pvd = new byte[SectorSize];
                int read = fs.Read(pvd, 0, SectorSize);

                if (read < SectorSize)
                    return new PvdInfo();

                return new PvdInfo
                {
                    Identifier = SafeGetString(pvd, 1, 5),
                    VolumeName = SafeGetString(pvd, 40, 32).Trim(),
                    SystemId = SafeGetString(pvd, 8, 32).Trim()
                };
            }
            catch
            {
                return new PvdInfo();
            }
        }

        private static string SafeGetString(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || offset + count > buffer.Length)
                return "";
            return Encoding.ASCII.GetString(buffer, offset, count);
        }

        private static Dictionary<string, (int lba, int size)> ReadRootDirectory(FileStream fs)
        {
            var files = new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase);

            try
            {
                fs.Seek(16 * SectorSize, SeekOrigin.Begin);
                byte[] pvd = new byte[SectorSize];
                int read = fs.Read(pvd, 0, SectorSize);

                if (read < SectorSize)
                    return files;

                int rootLba = BitConverter.ToInt32(pvd, 156 + 2);
                if (rootLba <= 0)
                    return files;

                byte[] sector = ReadSector(fs, rootLba);
                int pos = 0;

                while (pos < sector.Length)
                {
                    int len = sector[pos];
                    if (len == 0)
                        break;

                    if (pos + 33 >= sector.Length)
                        break;

                    int nameLen = sector[pos + 32];
                    if (pos + 33 + nameLen > sector.Length)
                        break;

                    string name = Encoding.ASCII.GetString(sector, pos + 33, nameLen)
                        .TrimEnd(';', '1');

                    int lba = BitConverter.ToInt32(sector, pos + 2);
                    int size = BitConverter.ToInt32(sector, pos + 10);

                    if (!string.IsNullOrWhiteSpace(name) && lba > 0)
                        files[name] = (lba, size);

                    pos += len;
                }
            }
            catch
            {
                // Si falla, devolver diccionario vacío
            }

            return files;
        }

        private static byte[]? ReadFile(FileStream fs,
            Dictionary<string, (int lba, int size)> files,
            string target)
        {
            if (!files.TryGetValue(target, out var entry))
                return null;

            try
            {
                int sectors = (entry.size + SectorSize - 1) / SectorSize;
                return ReadSector(fs, entry.lba, sectors);
            }
            catch
            {
                return null;
            }
        }

        private static byte[] ReadSector(FileStream fs, int lba, int count = 1)
        {
            int bytesToRead = count * SectorSize;
            byte[] buffer = new byte[bytesToRead];

            fs.Seek((long)lba * SectorSize, SeekOrigin.Begin);
            int read = fs.Read(buffer, 0, bytesToRead);

            if (read == 0)
                return Array.Empty<byte>();

            if (read < bytesToRead)
            {
                byte[] trimmed = new byte[read];
                Array.Copy(buffer, trimmed, read);
                return trimmed;
            }

            return buffer;
        }
    }

    /// <summary>
    /// Información extraída de un archivo ISO.
    /// </summary>
    public class IsoInfo
    {
        public string Path { get; set; } = "";
        public long SizeBytes { get; set; }
        public string? GameId { get; set; }
        public string Region { get; set; } = "Unknown";

        public PvdInfo? Pvd { get; set; }
        public Dictionary<string, (int lba, int size)> Files { get; set; } = new();
        public byte[]? SystemCnf { get; set; }
        public byte[]? IoprpImg { get; set; }
    }

    /// <summary>
    /// Primary Volume Descriptor de un ISO.
    /// </summary>
    public class PvdInfo
    {
        public string Identifier { get; set; } = "";
        public string VolumeName { get; set; } = "";
        public string SystemId { get; set; } = "";
    }
}