using System;
using System.Collections.Generic;
using System.IO;
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

        public SettingsService(string appDataFolder)
        {
            string folder = Path.Combine(appDataFolder, "POPSManager");
            Directory.CreateDirectory(folder);
            _settingsPath = Path.Combine(folder, "settings.json");
            Load();
        }

        public Task SaveAsync()
        {
            // Guardar como archivo de texto simple (JSON escrito a mano) para evitar problemas de trimming
            using var writer = new StreamWriter(_settingsPath, false);
            writer.WriteLine("{");
            WriteValue(writer, "SourceFolder", SourceFolder);
            WriteValue(writer, "DestinationFolder", DestinationFolder);
            WriteValue(writer, "ElfFolder", ElfFolder);
            WriteValue(writer, "RootFolder", RootFolder);
            WriteValue(writer, "UseDatabase", UseDatabase ? "true" : "false");
            WriteValue(writer, "UseCovers", UseCovers ? "true" : "false");
            WriteValue(writer, "UseMetadata", UseMetadata ? "true" : "false");
            WriteValue(writer, "UseTitleInElfName", UseTitleInElfName ? "true" : "false");
            // Automation se guarda como un objeto JSON simple
            writer.WriteLine("  \"Automation\": {");
            WriteValue(writer, "AutoConvert", Automation.AutoConvert ? "true" : "false");
            WriteValue(writer, "AutoProcess", Automation.AutoProcess ? "true" : "false");
            writer.WriteLine("  }");
            writer.WriteLine("}");

            OnSettingsChanged?.Invoke();
            return Task.CompletedTask;
        }

        private static void WriteValue(StreamWriter writer, string key, string? value)
        {
            writer.Write("  \"");
            writer.Write(key);
            writer.Write("\": ");
            if (value == null)
                writer.WriteLine("null,");
            else
            {
                writer.Write("\"");
                writer.Write(value.Replace("\\", "\\\\").Replace("\"", "\\\""));
                writer.WriteLine("\",");
            }
        }

        public void Load()
        {
            if (!File.Exists(_settingsPath)) return;

            try
            {
                var data = new Dictionary<string, string>();
                foreach (var line in File.ReadAllLines(_settingsPath))
                {
                    int colon = line.IndexOf(':');
                    if (colon <= 1) continue;
                    string key = line.Substring(0, colon).Trim().Trim('"');
                    string val = line.Substring(colon + 1).Trim().Trim(',').Trim('"');
                    if (key == "Automation")
                    {
                        // Se leerían más líneas, pero por simplicidad ignoramos y mantenemos valores por defecto
                        continue;
                    }
                    data[key] = val;
                }

                SourceFolder = data.TryGetValue("SourceFolder", out var src) ? src : null;
                DestinationFolder = data.TryGetValue("DestinationFolder", out var dst) ? dst : null;
                ElfFolder = data.TryGetValue("ElfFolder", out var elf) ? elf : null;
                RootFolder = data.TryGetValue("RootFolder", out var r) ? r : "";
                UseDatabase = data.TryGetValue("UseDatabase", out var db) ? db == "true" : true;
                UseCovers = data.TryGetValue("UseCovers", out var cv) ? cv == "true" : true;
                UseMetadata = data.TryGetValue("UseMetadata", out var meta) ? meta == "true" : true;
                UseTitleInElfName = data.TryGetValue("UseTitleInElfName", out var t) ? t == "true" : true;
            }
            catch { }
        }
    }
}