using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using POPSManager.Core.Logic;
using POPSManager.Core.Settings;

namespace POPSManager.Core.Logic.Cheats
{
    public class CheatManagerService
    {
        private readonly CheatSettingsService _settings;
        private readonly Action<string>? _log;

        public CheatManagerService(CheatSettingsService settings, Action<string>? log = null)
        {
            _settings = settings;
            _log = log;
        }

        public List<string> LoadCheatFile(string cheatPath)
        {
            if (!File.Exists(cheatPath)) return new List<string>();
            try
            {
                return File.ReadAllLines(cheatPath)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                _log?.Invoke($"[Cheats] Error leyendo CHEAT.TXT: {ex.Message}");
                return new List<string>();
            }
        }

        public void SaveCheatFile(string cheatPath, IEnumerable<string> cheats)
        {
            try
            {
                File.WriteAllLines(cheatPath, cheats);
                _log?.Invoke($"[Cheats] CHEAT.TXT guardado → {cheatPath}");
            }
            catch (Exception ex) { _log?.Invoke($"[Cheats] Error guardando CHEAT.TXT: {ex.Message}"); }
        }

        public List<string> MergeCheats(IEnumerable<string> existing, IEnumerable<string> autoFixes, IEnumerable<string> userSelected)
        {
            var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in existing) merged.Add(c);
            if (_settings.Current.UseAutoGameFixes)
                foreach (var c in autoFixes) merged.Add(c);
            foreach (var c in userSelected) merged.Add(c);
            return merged.ToList();
        }

        public List<string> GenerateAutoFixes(string gameId, string cd1Folder)
        {
            CheatGenerator.GenerateCheatTxt(gameId, cd1Folder, msg => _log?.Invoke(msg));
            string cheatPath = Path.Combine(cd1Folder, "CHEAT.TXT");
            if (!File.Exists(cheatPath)) return new List<string>();
            try
            {
                var auto = File.ReadAllLines(cheatPath).Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                File.Delete(cheatPath);
                return auto;
            }
            catch (Exception ex)
            {
                _log?.Invoke($"[Cheats] Error leyendo CHEAT.TXT generado: {ex.Message}");
                return new List<string>();
            }
        }

        public void SaveUserCheats(string rootFolder, IEnumerable<CheatDefinition> customCheats)
        {
            try
            {
                string folder = Path.Combine(rootFolder, "Cheats");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, "UserCheats.json");
                var json = JsonSerializer.Serialize(customCheats, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                _log?.Invoke("[Cheats] Cheats personalizados guardados.");
            }
            catch (Exception ex) { _log?.Invoke($"[Cheats] Error guardando cheats personalizados: {ex.Message}"); }
        }

        public List<CheatDefinition> LoadUserCheats(string rootFolder)
        {
            try
            {
                string path = Path.Combine(rootFolder, "Cheats", "UserCheats.json");
                if (!File.Exists(path)) return new List<CheatDefinition>();
                return JsonSerializer.Deserialize<List<CheatDefinition>>(File.ReadAllText(path)) ?? new List<CheatDefinition>();
            }
            catch (Exception ex)
            {
                _log?.Invoke($"[Cheats] Error cargando cheats personalizados: {ex.Message}");
                return new List<CheatDefinition>();
            }
        }
    }
}