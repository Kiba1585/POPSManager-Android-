using System.IO;

namespace POPSManager.Core.Logic.MultiDisc
{
    public sealed class DiscInfo
    {
        public string Path { get; set; } = "";
        public int DiscNumber { get; set; }
        public string GameId { get; set; } = "";
        public string FolderName { get; set; } = "";
        public string FileName { get; set; } = "";

        public string FileNameNoExt => System.IO.Path.GetFileNameWithoutExtension(FileName) ?? "";
        public string FolderPath => System.IO.Path.GetDirectoryName(Path) ?? "";
    }
}