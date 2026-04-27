using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using POPSManager.Core.Settings;

namespace POPSManager.Core.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;

        public string? SourceFolder { get; set; }
        public string? DestinationFolder { get; set; }
        public string? ElfFolder { get; set; }
        public string RootFolder { get; set; } = "";
        public bool UseDatabase { get; set; } = true;
        public bool UseCovers { get; set; } = true;
        public bool UseMetadata { get; set; } = true;
        public bool UseTitleInElfName { get; set; } = true;
        public AutomationSettings Automation { get; set; } = new();

        public event Action? OnSettingsChanged;

        public SettingsService()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "POPSManager");
            Directory.CreateDirectory(folder);
            _settingsPath = Path.Combine(folder, "settings.json");
            Load();
        }

        public async Task SaveAsync()
        {
            var data = new
            {
                SourceFolder, DestinationFolder, ElfFolder, RootFolder,
                UseDatabase, UseCovers, UseMetadata, UseTitleInElfName,
                Automation
            };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsPath, json);
            OnSettingsChanged?.Invoke();
        }

        public void Load()
        {
            if (!File.Exists(_settingsPath)) return;
            try
            {
                var json = File.ReadAllText(_settingsPath);
                var data = JsonSerializer.Deserialize<SettingsData>(json);
                if (data != null)
                {
                    SourceFolder = data.SourceFolder;
                    DestinationFolder = data.DestinationFolder;
                    ElfFolder = data.ElfFolder;
                    RootFolder = data.RootFolder ?? "";
                    UseDatabase = data.UseDatabase;
                    UseCovers = data.UseCovers;
                    UseMetadata = data.UseMetadata;
                    UseTitleInElfName = data.UseTitleInElfName;
                    Automation = data.Automation ?? new();
                }
            }
            catch { }
        }

        private class SettingsData
        {
            public string? SourceFolder { get; set; }
            public string? DestinationFolder { get; set; }
            public string? ElfFolder { get; set; }
            public string? RootFolder { get; set; }
            public bool UseDatabase { get; set; } = true;
            public bool UseCovers { get; set; } = true;
            public bool UseMetadata { get; set; } = true;
            public bool UseTitleInElfName { get; set; } = true;
            public AutomationSettings? Automation { get; set; }
        }
    }
}