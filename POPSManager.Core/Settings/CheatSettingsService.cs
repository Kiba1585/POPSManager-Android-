using System;
using System.IO;
using System.Text.Json;

namespace POPSManager.Core.Settings;

public class CheatSettingsService
{
    private readonly string _settingsPath;
    public CheatSettings Current { get; private set; } = new();

    public CheatSettingsService(string rootFolder, Action<string>? log = null)
    {
        var settingsFolder = Path.Combine(rootFolder, "Settings");
        Directory.CreateDirectory(settingsFolder);
        _settingsPath = Path.Combine(settingsFolder, "CheatSettings.json");

        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var loaded = JsonSerializer.Deserialize<CheatSettings>(json);
                if (loaded != null) Current = loaded;
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"[Cheats] Error cargando CheatSettings: {ex.Message}");
        }
    }

    public void Save(Action<string>? log = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
            log?.Invoke("[Cheats] CheatSettings guardado.");
        }
        catch (Exception ex)
        {
            log?.Invoke($"[Cheats] Error guardando CheatSettings: {ex.Message}");
        }
    }
}
