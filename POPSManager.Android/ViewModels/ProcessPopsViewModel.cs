using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using POPSManager.Core.Logic;
using POPSManager.Core.Logic.Covers;
using POPSManager.Core.Services;
using POPSManager.Android.Services;

namespace POPSManager.Android.ViewModels;

public class ProcessPopsViewModel : BindableObject
{
    private readonly IPathsService _paths;
    private readonly ILoggingService _log;
    private readonly SettingsService _settings;

    public ObservableCollection<GameEntry> Ps1Games { get; } = new();
    public ObservableCollection<GameEntry> Ps2Games { get; } = new();
    public ObservableCollection<GameEntry> AppsGames { get; } = new();

    public class GameEntry
    {
        public string GameId { get; set; } = "";
        public string Name { get; set; } = "";
        public string FolderPath { get; set; } = "";
        public string VcdPath { get; set; } = "";
        public bool IsMultiDisc { get; set; }
        public int DiscNumber { get; set; } = 1;
        public override string ToString() => Name;
    }

    private bool _cheatWidescreen;
    private bool _cheatNoPal;
    private bool _cheatFixSound;
    private bool _cheatFixGraphics;

    public bool CheatWidescreen { get => _cheatWidescreen; set => SetProperty(ref _cheatWidescreen, value); }
    public bool CheatNoPal { get => _cheatNoPal; set => SetProperty(ref _cheatNoPal, value); }
    public bool CheatFixSound { get => _cheatFixSound; set => SetProperty(ref _cheatFixSound, value); }
    public bool CheatFixGraphics { get => _cheatFixGraphics; set => SetProperty(ref _cheatFixGraphics, value); }

    private string _oplRootFolder = "";
    private string _status = "";

    public ICommand SelectOplRootFolderCommand { get; }
    public ICommand ProcessAllCommand { get; }
    public ICommand GenerateElfCommand { get; }
    public ICommand GenerateCheatsCommand { get; }
    public ICommand DownloadCoversAndMetadataCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand RenameAllCommand { get; }        // ← NUEVO COMANDO

    public string OplRootFolder { get => _oplRootFolder; set => SetProperty(ref _oplRootFolder, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public ProcessPopsViewModel(IPathsService paths, ILoggingService log, SettingsService settings)
    {
        _paths = paths;
        _log = log;
        _settings = settings;

        SelectOplRootFolderCommand = new Command(async () => await SelectOplRootFolder());
        ProcessAllCommand = new Command(async () => await ProcessAllGames());
        GenerateElfCommand = new Command(async () => await GenerateAllElfs());
        GenerateCheatsCommand = new Command(async () => await GenerateAllCheats());
        DownloadCoversAndMetadataCommand = new Command(async () => await DownloadAllCoversAndMetadata());
        RefreshCommand = new Command(RefreshGameLists);
        RenameAllCommand = new Command(async () => await RenameAllGames());   // ← NUEVO

        RefreshFromSettings();
    }

    // ... (todos los métodos existentes se mantienen igual) ...

    /// <summary>
    /// Renombra las carpetas de PS1 y los archivos ISO de PS2 con el formato "GAMEID - Nombre".
    /// </summary>
    private async Task RenameAllGames()
    {
        if (!Ps1Games.Any() && !Ps2Games.Any())
        {
            Status = "No hay juegos para renombrar.";
            return;
        }

        int renamedCount = 0;
        var errors = new List<string>();

        // Renombrar PS1 (carpetas dentro de POPS)
        foreach (var game in Ps1Games)
        {
            try
            {
                string folder = game.FolderPath;
                if (!Directory.Exists(folder)) continue;

                string gameId = game.GameId;
                string cleanName = NormalizeGameTitle(Path.GetFileName(folder), game.DiscNumber, true);
                string newFolderName = $"{gameId} - {cleanName}";
                string parentDir = Path.GetDirectoryName(folder)!;
                string newFolderPath = Path.Combine(parentDir, newFolderName);

                if (string.Equals(folder, newFolderPath, StringComparison.OrdinalIgnoreCase))
                    continue; // ya tiene el nombre correcto

                Directory.Move(folder, newFolderPath);
                game.FolderPath = newFolderPath;

                // Actualizar ruta del VCD si existe
                if (!string.IsNullOrEmpty(game.VcdPath))
                {
                    string oldVcdFileName = Path.GetFileName(game.VcdPath);
                    game.VcdPath = Path.Combine(newFolderPath, oldVcdFileName);
                }

                renamedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"{game.Name}: {ex.Message}");
            }
        }

        // Renombrar PS2 (archivos ISO dentro de DVD)
        foreach (var game in Ps2Games)
        {
            try
            {
                string filePath = game.FolderPath;
                if (!File.Exists(filePath)) continue;

                string gameId = game.GameId;
                string cleanName = NormalizeGameTitle(Path.GetFileNameWithoutExtension(filePath), 1, false);
                string newFileName = $"{gameId} - {cleanName}.ISO";
                string parentDir = Path.GetDirectoryName(filePath)!;
                string newFilePath = Path.Combine(parentDir, newFileName);

                if (string.Equals(filePath, newFilePath, StringComparison.OrdinalIgnoreCase))
                    continue;

                File.Move(filePath, newFilePath);
                game.FolderPath = newFilePath;
                renamedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"{game.Name}: {ex.Message}");
            }
        }

        // Refrescar las listas para mostrar los nuevos nombres
        RefreshGameLists();

        if (errors.Any())
            Status = $"Renombrados: {renamedCount}. Errores: {string.Join("; ", errors)}";
        else
            Status = $"{renamedCount} juegos renombrados correctamente.";
    }

    // El resto del código (SetProperty, etc.) sigue igual.
}