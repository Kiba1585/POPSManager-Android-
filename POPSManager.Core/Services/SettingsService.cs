using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace POPSManager.Core.Services;

public class SettingsService
{
    private readonly string _settingsPath;

    public string? SourceFolder { get; set; }
    public string? DestinationFolder { get; set; }
    public string? ElfFolder { get; set; }
    public string RootFolder { get; set; } = "";

    public SettingsService()
    {
        string folder = Path.Combine(FileSystem.AppDataDirectory, "POPSManager");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public async Task SaveAsync()
    {
        var data = new { SourceFolder, DestinationFolder, ElfFolder, RootFolder };
        var json = JsonSerializer.Serialize(data);
        await File.WriteAllTextAsync(_settingsPath, json);
    }

    public void Load()
    {
        if (!File.Exists(_settingsPath)) return;
        var json = File.ReadAllText(_settingsPath);
        var data = JsonSerializer.Deserialize<SettingsData>(json);
        if (data != null)
        {
            SourceFolder = data.SourceFolder;
            DestinationFolder = data.DestinationFolder;
            ElfFolder = data.ElfFolder;
            RootFolder = data.RootFolder ?? "";
        }
    }

    private class SettingsData
    {
        public string? SourceFolder { get; set; }
        public string? DestinationFolder { get; set; }
        public string? ElfFolder { get; set; }
        public string? RootFolder { get; set; }
    }
}
