using System;
using System.IO;

namespace POPSManager.Core.Logic
{
    public static class IntegrityValidator
    {
        private const int HeaderSize = 0x800;
        private const int VcdSectorSize = 2048;

        public static bool Validate(string vcdPath, Action<string>? log = null)
        {
            if (!File.Exists(vcdPath))
            {
                log?.Invoke($"[Integrity] ERROR: Archivo no encontrado → {vcdPath}");
                return false;
            }

            var info = new FileInfo(vcdPath);
            if (info.Length < HeaderSize + VcdSectorSize)
            {
                log?.Invoke("[Integrity] ERROR: VCD demasiado pequeño.");
                return false;
            }

            try
            {
                using var fs = File.OpenRead(vcdPath);
                byte[] header = new byte[HeaderSize];
                if (fs.Read(header, 0, HeaderSize) != HeaderSize)
                {
                    log?.Invoke("[Integrity] ERROR: No se pudo leer el header completo.");
                    return false;
                }

                if (header[0] != 'P' || header[1] != 'S' || header[2] != 'X')
                {
                    log?.Invoke("[Integrity] ERROR: Header VCD inválido.");
                    return false;
                }

                int sectors = BitConverter.ToInt32(header, 0x10);
                if (sectors <= 0)
                {
                    log?.Invoke("[Integrity] ERROR: Número de sectores inválido.");
                    return false;
                }

                long expectedSize = HeaderSize + (long)sectors * VcdSectorSize;
                if (expectedSize != info.Length)
                {
                    log?.Invoke($"[Integrity] ERROR: Tamaño incorrecto. Esperado: {expectedSize}, Actual: {info.Length}");
                    return false;
                }

                byte[] sector = new byte[VcdSectorSize];
                long sectorCount = 0;
                while (fs.Read(sector, 0, VcdSectorSize) == VcdSectorSize) sectorCount++;
                if (sectorCount != sectors)
                {
                    log?.Invoke($"[Integrity] ERROR: Conteo de sectores incorrecto.");
                    return false;
                }

                log?.Invoke("[Integrity] OK: VCD válido y consistente.");
                return true;
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Integrity] ERROR leyendo VCD: {ex.Message}");
                return false;
            }
        }
    }
}