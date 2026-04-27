using System;
using System.IO;
using System.Text;

namespace POPSManager.Core.Logic.Converter
{
    public static class VcdHeader
    {
        public static void Write(FileStream output, string name, long binSize, Action<string> log)
        {
            byte[] header = new byte[0x800];
            Array.Copy(Encoding.ASCII.GetBytes("PSX"), 0, header, 0, 3);

            int sectors = (int)(binSize / 2352);
            BitConverter.GetBytes(sectors).CopyTo(header, 0x10);

            var label = Encoding.ASCII.GetBytes(name.ToUpperInvariant());
            Array.Copy(label, 0, header, 0x20, Math.Min(label.Length, 32));

            output.Write(header, 0, header.Length);
            log("[VCD] Header escrito correctamente");
        }
    }
}