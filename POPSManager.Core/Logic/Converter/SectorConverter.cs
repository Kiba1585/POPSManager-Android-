using System;
using System.IO;

namespace POPSManager.Core.Logic.Converter
{
    public static class SectorConverter
    {
        public static void Convert(FileStream input, FileStream output, SectorMode mode,
                                   Action<int> updateProgress, Action<string> log)
        {
            int sectorSize = mode == SectorMode.Raw2448 ? 2448 : 2352;
            long total = input.Length / sectorSize;
            long processed = 0;

            byte[] sector = new byte[sectorSize];
            byte[] userData = new byte[2048];

            input.Seek(0, SeekOrigin.Begin);

            while (input.Read(sector, 0, sectorSize) == sectorSize)
            {
                ExtractUserData(sector, userData, mode);
                output.Write(userData, 0, 2048);

                processed++;
                if (processed % 200 == 0)
                {
                    int percent = (int)((processed / (double)total) * 100);
                    updateProgress(percent);
                }
            }

            log($"[VCD] Conversión completada ({processed} sectores)");
        }

        private static void ExtractUserData(byte[] sector, byte[] userData, SectorMode mode)
        {
            switch (mode)
            {
                case SectorMode.Mode1: Array.Copy(sector, 16, userData, 0, 2048); break;
                default: Array.Copy(sector, 24, userData, 0, 2048); break;
            }
        }
    }
}