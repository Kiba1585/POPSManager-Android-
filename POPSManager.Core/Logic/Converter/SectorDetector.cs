using System;
using System.IO;

namespace POPSManager.Logic
{
    public enum SectorMode { Mode1, Mode2Form1, Mode2Form2, Raw2448, Unknown }

    public static class SectorDetector
    {
        public static SectorMode Detect(FileStream input, Action<string> log)
        {
            byte[] buffer = new byte[2448];
            input.Seek(0, SeekOrigin.Begin);

            int read = input.Read(buffer, 0, buffer.Length);
            if (read < 2352)
                return SectorMode.Unknown;

            if (read == 2448)
                return SectorMode.Raw2448;

            if (buffer[15] == 0x01)
                return SectorMode.Mode1;

            if (buffer[15] == 0x02)
            {
                int form = buffer[18];
                if (form == 0x02)
                    return SectorMode.Mode2Form2;

                return SectorMode.Mode2Form1;
            }

            return SectorMode.Unknown;
        }
    }
}