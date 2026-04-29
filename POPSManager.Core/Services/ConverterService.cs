using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POPSManager.Core.Services;

public class ConverterService
{
    private readonly Action<string>? _log;
    private readonly Action<string>? _setStatus;

    private const int SectorSize = 2352;
    private const int UserDataSize = 2048;
    private const int HeaderSize = 0x800;
    private const int BufferSize = 1024 * 1024;

    public ConverterService(Action<string>? log = null, Action<string>? setStatus = null)
    {
        _log = log;
        _setStatus = setStatus;
    }

    public async Task ConvertFolderAsync(string sourceFolder, string outputFolder)
    {
        if (!Directory.Exists(sourceFolder))
            throw new DirectoryNotFoundException($"Origen no encontrado: {sourceFolder}");

        Directory.CreateDirectory(outputFolder);

        // Solo convertimos archivos .bin (imágenes PS1)
        var files = Directory.GetFiles(sourceFolder, "*.bin", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToArray();

        foreach (var file in files)
        {
            try
            {
                await ConvertToVcdAsync(file, outputFolder);
            }
            catch (Exception ex)
            {
                _log?.Invoke($"ERROR convirtiendo {file}: {ex.Message}");
            }
        }
    }

    private async Task ConvertToVcdAsync(string inputPath, string outputFolder)
    {
        string name = Path.GetFileNameWithoutExtension(inputPath);
        string outputPath = Path.Combine(outputFolder, name + ".vcd");

        await ConvertPs1ToVcdAsync(inputPath, outputPath, name);
    }

    private async Task ConvertPs1ToVcdAsync(string inputPath, string outputPath, string name)
    {
        await using var input = File.OpenRead(inputPath);
        await using var output = File.Create(outputPath);

        byte[] header = new byte[HeaderSize];
        Array.Copy(Encoding.ASCII.GetBytes("PSX"), header, 3);
        await output.WriteAsync(header, 0, header.Length);

        byte[] sector = new byte[SectorSize];
        byte[] outputBuffer = new byte[BufferSize];
        int bufferPos = 0;
        long totalSectors = input.Length / SectorSize;
        long processed = 0;

        while (true)
        {
            int read = await input.ReadAsync(sector, 0, SectorSize);
            if (read == 0) break;
            if (read != SectorSize) break;

            if (bufferPos + UserDataSize > outputBuffer.Length)
            {
                await output.WriteAsync(outputBuffer, 0, bufferPos);
                bufferPos = 0;
            }

            Buffer.BlockCopy(sector, 24, outputBuffer, bufferPos, UserDataSize);
            bufferPos += UserDataSize;
            processed++;

            if (processed % 200 == 0)
            {
                int percent = (int)((processed / (double)totalSectors) * 100);
                _setStatus?.Invoke($"Convirtiendo {name}: {percent}%");
            }
        }

        if (bufferPos > 0)
            await output.WriteAsync(outputBuffer, 0, bufferPos);
    }
}