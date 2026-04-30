namespace POPSManager.Core.Services;

public interface IPathsService
{
    string RootFolder { get; set; }
    string PopsFolder { get; }
    string AppsFolder { get; }
    string CfgFolder { get; }
    string ArtFolder { get; }
    string DvdFolder { get; }
    string PopstarterElfPath { get; set; }
    string PopstarterPs2ElfPath { get; set; }
    string TempFolder { get; }

    Task<string?> SelectFolderAsync();
    Task<string?> SelectFileAsync(string filter);

    void SetElfPath(string path);
    void SetPs2ElfPath(string path);

    /// <summary>Abre una carpeta con un explorador de archivos.</summary>
    void OpenFolder(string folderPath);
}