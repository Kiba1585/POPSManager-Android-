using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

    // Colecciones de juegos (ahora con nombre visible)
    public ObservableCollection<GameEntry> Ps1Games { get; } = new();
    public ObservableCollection<GameEntry> Ps2Games { get; } = new();
    public ObservableCollection<GameEntry> AppsGames { get; } = new();

    public class GameEntry
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public override string ToString() => Name;
    }

    // Opciones de cheats (bindable)
    private bool _cheatWidescreen = false;
    private bool _cheatForcePal60 = false;
    private bool _cheatFixSound = false;
    private bool _cheatFixGraphics = false;

    public bool CheatWidescreen { get => _cheatWidescreen; set => SetProperty(ref _cheatWidescreen, value); }
    public bool CheatForcePal60 { get => _cheatForcePal60; set => SetProperty(ref _cheatForcePal60, value); }
    public bool CheatFixSound { get => _cheatFixSound; set => SetProperty(ref _cheatFixSound, value); }
    public bool CheatFixGraphics { get => _cheatFixGraphics; set => SetProperty(ref _cheatFixGraphics, value); }

    private string _oplRootFolder = "";
    private string _status = "";

    public ICommand SelectOplRootFolderCommand { get; }
    public ICommand ProcessAllCommand { get; }
    public ICommand GenerateElfCommand { get; }
    public ICommand GenerateCheatsCommand { get; }
    public ICommand DownloadCoversCommand { get; }
    public ICommand RefreshCommand { get; }

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
        DownloadCoversCommand = new Command(async () => await DownloadAllCovers());
        RefreshCommand = new Command(RefreshGameLists);

        RefreshFromSettings();
    }

    public void RefreshFromSettings()
    {
        var savedRoot = _settings.DestinationFolder;
        if (!string.IsNullOrEmpty(savedRoot))
        {
            if (savedRoot != _oplRootFolder)
            {
                _oplRootFolder = savedRoot;
                OnPropertyChanged(nameof(OplRootFolder));
                if (_paths is PathsServiceAndroid androidPaths) androidPaths.RootFolder = savedRoot;
            }
            Status = $"Raíz OPL: {savedRoot}";
            RefreshGameLists();
        }
        else Status = "Selecciona la carpeta raíz OPL (desde Inicio o aquí).";
    }

    private async Task SelectOplRootFolder()
    {
        var path = await _paths.SelectFolderAsync();
        if (path != null)
        {
            _settings.DestinationFolder = path;
            _settings.RootFolder = path;
            await _settings.SaveAsync();
            OplRootFolder = path;
            if (_paths is PathsServiceAndroid androidPaths) androidPaths.RootFolder = path;
            RefreshGameLists();
        }
    }

    private void RefreshGameLists()
    {
        Ps1Games.Clear();
        Ps2Games.Clear();
        AppsGames.Clear();
        try
        {
            if (Directory.Exists(_paths.PopsFolder))
                foreach (var dir in Directory.GetDirectories(_paths.PopsFolder))
                    Ps1Games.Add(new GameEntry { Name = Path.GetFileName(dir), Path = dir, Id = ExtractGameId(dir) });

            if (Directory.Exists(_paths.DvdFolder))
                foreach (var file in Directory.GetFiles(_paths.DvdFolder, "*.ISO"))
                    Ps2Games.Add(new GameEntry { Name = Path.GetFileNameWithoutExtension(file), Path = file, Id = ExtractGameId(file) });

            if (Directory.Exists(_paths.AppsFolder))
                foreach (var file in Directory.GetFiles(_paths.AppsFolder, "*.ELF"))
                    AppsGames.Add(new GameEntry { Name = Path.GetFileNameWithoutExtension(file), Path = file, Id = ExtractGameId(file) });

            Status = $"Juegos: {Ps1Games.Count} PS1, {Ps2Games.Count} PS2, {AppsGames.Count} APPs";
        }
        catch (Exception ex) { Status = $"Error al listar: {ex.Message}"; }
    }

    private string ExtractGameId(string path)
    {
        // Intentar usar el detector de IDs del Core; en su defecto usamos el nombre.
        try { return GameIdDetector.GetGameIdFromPath(path) ?? Path.GetFileNameWithoutExtension(path); }
        catch { return Path.GetFileNameWithoutExtension(path); }
    }

    private async Task ProcessAllGames()
    {
        await GenerateAllElfs();
        await GenerateAllCheats();
        await DownloadAllCovers();
        Status = "Procesamiento completo.";
    }

    private async Task GenerateAllElfs()
    {
        Status = "Generando ELFs...";
        string baseElf = _paths.PopstarterElfPath;
        if (string.IsNullOrEmpty(baseElf) || !File.Exists(baseElf))
        {
            Status = "POPSTARTER.ELF no encontrado. Configúralo en Inicio.";
            return;
        }

        foreach (var game in Ps1Games)
        {
            string vcd = Directory.GetFiles(game.Path, "*.VCD").FirstOrDefault();
            if (vcd == null)
            {
                _log.Log($"[ELF] No se encontró VCD en {game.Name}");
                continue;
            }
            // El GameIdDetector puede devolver el ID real; si no, usamos el extraído
            string gameId = ExtractGameId(game.Path);
            string cleanTitle = game.Name; // podríamos limpiarlo más
            ElfGenerator.GeneratePs1Elf(baseElf, vcd, _paths.AppsFolder, 1, cleanTitle, gameId, msg => _log.Log(msg));
        }
        Status = "ELFs generados.";
    }

    private async Task GenerateAllCheats()
    {
        Status = "Generando cheats...";
        var extraLines = new List<string>();
        if (CheatWidescreen) extraLines.Add("WIDESCREEN=ON");
        if (CheatForcePal60) extraLines.Add("FORCEVIDEO=1");
        if (CheatFixSound) extraLines.Add("FIXSOUND=ON");
        if (CheatFixGraphics) extraLines.Add("FIXGRAPHICS=ON");

        foreach (var game in Ps1Games)
        {
            string gameId = ExtractGameId(game.Path);
            CheatGenerator.GenerateCheatTxt(gameId, game.Path, extraLines, msg => _log.Log(msg));
        }
        Status = "Cheats generados.";
    }

    private async Task DownloadAllCovers()
    {
        Status = "Descargando covers...";
        foreach (var game in Ps1Games.Concat(Ps2Games))
        {
            string gameId = game.Id;
            string? coverUrl = GameDatabase.TryGetCoverUrl(gameId); // Método a implementar en GameDatabase
            if (coverUrl != null)
                await ArtDownloader.DownloadArtAsync(gameId, coverUrl, _paths.ArtFolder, msg => _log.Log(msg));
            else
                _log.Log($"[COVER] Sin URL para {gameId}");
        }
        Status = "Covers actualizados.";
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}