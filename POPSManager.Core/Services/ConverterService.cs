using System;
using System.IO;
using System.Threading.Tasks;

namespace POPSManager.Core.Services;

public class ConverterService
{
    public async Task ConvertFolder(string sourceFolder, string outputFolder)
    {
        if (!Directory.Exists(sourceFolder))
            throw new DirectoryNotFoundException($"Source folder not found: {sourceFolder}");

        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        var files = Directory.GetFiles(sourceFolder, "*.*", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            if (file.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
            {
                await ConvertFile(file, outputFolder);
            }
        }
    }

    public async Task ConvertFile(string inputPath, string outputFolder)
    {
        string fileName = Path.GetFileNameWithoutExtension(inputPath);
        string outputPath = Path.Combine(outputFolder, fileName + ".vcd");

        // Simulación de trabajo
        await Task.Delay(300);

        // Crear archivo vacío para que compile
        File.WriteAllText(outputPath, "VCD-DATA");
    }
}