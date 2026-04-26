using System;
using System.IO;

namespace POPSManager.Logic
{
    public static class IntegrityValidator
    {
        private const int HeaderSize = 0x800;
        private const int VcdSectorSize = 2048;

        // ============================================================
        //  VALIDAR VCD COMPLETO (ULTRA PRO)
        // ============================================================
        public static bool Validate(string vcdPath, Action<string>? log = null)
        {
            if (!File.Exists(vcdPath))
            {
                log?.Invoke($"[Integrity] ERROR: Archivo no encontrado → {vcdPath}");
                return false;
            }

            var info = new FileInfo(vcdPath);

            // Tamaño mínimo razonable
            if (info.Length < HeaderSize + VcdSectorSize)
            {
                log?.Invoke("[Integrity] ERROR: VCD demasiado pequeño.");
                return false;
            }

            try
            {
                using var fs = File.OpenRead(vcdPath);

                // Leer header
                byte[] header = new byte[HeaderSize];
                int readHeader = fs.Read(header, 0, HeaderSize);

                if (readHeader != HeaderSize)
                {
                    log?.Invoke("[Integrity] ERROR: No se pudo leer el header completo.");
                    return false;
                }

                // Validar firma "PSX"
                if (header[0] != 'P' || header[1] != 'S' || header[2] != 'X')
                {
                    log?.Invoke("[Integrity] ERROR: Header VCD inválido (firma incorrecta).");
                    return false;
                }

                // Leer número de sectores
                int sectors = BitConverter.ToInt32(header, 0x10);
                if (sectors <= 0)
                {
                    log?.Invoke("[Integrity] ERROR: Número de sectores inválido en el header.");
                    return false;
                }

                long expectedSize = HeaderSize + (long)sectors * VcdSectorSize;

                // Validar tamaño exacto
                if (expectedSize != info.Length)
                {
                    log?.Invoke($"[Integrity] ERROR: Tamaño incorrecto. Esperado: {expectedSize}, Actual: {info.Length}");
                    return false;
                }

                // Validar sectores uno por uno (opcional pero profesional)
                byte[] sector = new byte[VcdSectorSize];
                long sectorCount = 0;

                while (fs.Read(sector, 0, VcdSectorSize) == VcdSectorSize)
                {
                    sectorCount++;
                }

                if (sectorCount != sectors)
                {
                    log?.Invoke($"[Integrity] ERROR: Conteo de sectores incorrecto. Header: {sectors}, Reales: {sectorCount}");
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