using System;
using System.IO;
using System.Text;

namespace POPSManager.Logic
{
    public static class VcdHeader
    {
        public static void Write(FileStream output, string name, long binSize, Action<string> log)
        {
            byte[] header = new byte[0x800];

            // PSX signature
            Array.Copy(Encoding.ASCII.GetBytes("PSX"), 0, header, 0, 3);

            // Sector count
            int sectors = (int)(binSize / 2352);
            BitConverter.GetBytes(sectors).CopyTo(header, 0x10);

            // Game title (max 32 chars)
            var label = Encoding.ASCII.GetBytes(name.ToUpperInvariant());
            Array.Copy(label, 0, header, 0x20, Math.Min(label.Length, 32));

            // Write header
            output.Write(header, 0, header.Length);

            log("[VCD] Header escrito correctamente");
        }
    }
}