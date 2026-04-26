using System.IO;

namespace POPSManager.Logic
{
    /// <summary>
    /// Información de un disco individual en un conjunto multidisco.
    /// </summary>
    public sealed class DiscInfo
    {
        public string Path { get; set; } = string.Empty;
        public int DiscNumber { get; set; }
        public string GameId { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;

        public string FileNameNoExt =>
            System.IO.Path.GetFileNameWithoutExtension(FileName) ?? string.Empty;

        public string FolderPath =>
            System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

        public string CleanTitle =>
            NameCleanerBase.CleanTitleOnly(FileNameNoExt);

        public bool FolderMatchesDisc =>
            FolderName.StartsWith("CD", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(FolderName.AsSpan(2), out int n) &&
            n == DiscNumber;
    }
}