using System;
using System.IO;
using System.Threading.Tasks;

namespace POPSManager.Core.Services
{
    public class PathsService : IPathsService
    {
        public string RootFolder { get; set; } = "";
        public string PopsFolder => Path.Combine(RootFolder, "POPS");
        public string AppsFolder => Path.Combine(RootFolder, "APPS");
        public string CfgFolder => Path.Combine(RootFolder, "CFG");
        public string ArtFolder => Path.Combine(RootFolder, "ART");
        public string DvdFolder => Path.Combine(RootFolder, "DVD");
        public string LngFolder => Path.Combine(RootFolder, "LNG");
        public string ThmFolder => Path.Combine(RootFolder, "THM");
        public string PopstarterElfPath { get; set; } = "";
        public string PopstarterPs2ElfPath { get; set; } = "";
        public string TempFolder => Path.Combine(Path.GetTempPath(), "POPSManager");

        public Task<string?> SelectFolderAsync() => Task.FromResult<string?>(null);
        public Task<string?> SelectFileAsync(string filter) => Task.FromResult<string?>(null);

        public void SetElfPath(string path) => PopstarterElfPath = path;
        public void SetPs2ElfPath(string path) => PopstarterPs2ElfPath = path;

        /// <summary>En la versión de escritorio no se usa; solo cumple con la interfaz.</summary>
        public void OpenFolder(string folderPath) { /* sin acción en Core */ }
    }
}