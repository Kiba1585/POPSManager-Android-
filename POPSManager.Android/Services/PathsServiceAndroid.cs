using POPSManager.Core.Services;

namespace POPSManager.Android.Services;

public class PathsServiceAndroid : IPathsService
{
    public string RootFolder { get; set; } = "";
    public string PopsFolder => Path.Combine(RootFolder, "POPS");
    public string AppsFolder => Path.Combine(RootFolder, "APPS");
    public string CfgFolder => Path.Combine(RootFolder, "CFG");
    public string ArtFolder => Path.Combine(RootFolder, "ART");
    public string DvdFolder => Path.Combine(RootFolder, "DVD");
    public string PopstarterElfPath { get; set; } = "";
    public string PopstarterPs2ElfPath { get; set; } = "";
    public string TempFolder => FileSystem.CacheDirectory;

    public Task<string?> SelectFolderAsync()
    {
        // MAUI no tiene FolderPicker nativo. Usamos FilePicker para que el usuario seleccione un archivo dentro de la carpeta deseada.
        // Para una experiencia completa, se necesitaría un plugin como CommunityToolkit.Maui FolderPicker.
        return Task.FromResult<string?>(null);
    }

    public Task<string?> SelectFileAsync(string filter)
    {
        return Task.FromResult<string?>(null);
    }

    public void SetElfPath(string path) => PopstarterElfPath = path;
    public void SetPs2ElfPath(string path) => PopstarterPs2ElfPath = path;
}