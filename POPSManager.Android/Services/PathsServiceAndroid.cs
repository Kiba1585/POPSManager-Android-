using CommunityToolkit.Maui.Storage;
using POPSManager.Core.Services;

namespace POPSManager.Android.Services;

public class PathsServiceAndroid : IPathsService
{
    public string RootFolder { get; set; } = "/storage/emulated/0/POPSManager";
    public string PopsFolder => System.IO.Path.Combine(RootFolder, "POPS");
    public string AppsFolder => System.IO.Path.Combine(RootFolder, "APPS");
    public string CfgFolder => System.IO.Path.Combine(RootFolder, "CFG");
    public string ArtFolder => System.IO.Path.Combine(RootFolder, "ART");
    public string DvdFolder => System.IO.Path.Combine(RootFolder, "DVD");
    public string PopstarterElfPath { get; set; } = "";
    public string PopstarterPs2ElfPath { get; set; } = "";
    public string TempFolder => FileSystem.CacheDirectory;

    /// <summary>
    /// Carpeta interna de la aplicación donde siempre es posible escribir.
    /// Se puede usar como destino seguro cuando la ruta externa falle.
    /// </summary>
    public string SafeOutputFolder => System.IO.Path.Combine(FileSystem.AppDataDirectory, "Output");

    public async Task<string?> SelectFolderAsync()
    {
        var result = await FolderPicker.Default.PickAsync(default);
        return result?.Folder?.Path;
    }

    public async Task<string?> SelectFileAsync(string filter = "*/*")
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Selecciona un archivo",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { filter } }
            })
        });
        return result?.FullPath;
    }

    /// <summary>
    /// Comprueba si una carpeta permite escritura real.
    /// </summary>
    public bool IsFolderWritable(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return false;

        try
        {
            var testFile = System.IO.Path.Combine(folderPath, ".writetest");
            System.IO.File.WriteAllText(testFile, "test");
            System.IO.File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void SetElfPath(string path) => PopstarterElfPath = path;
    public void SetPs2ElfPath(string path) => PopstarterPs2ElfPath = path;
}