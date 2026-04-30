using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

    // Opciones de cheats actualizadas
    private bool _cheatWidescreen;
    private bool _cheatNoPal;          // en lugar de ForcePal60
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
            RefreshGameLists();
            Status = $"Raíz OPL: {savedRoot}";
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
            {
                foreach (var dir in Directory.GetDirectories(_paths.PopsFolder))
                    Ps1Games.Add(BuildPs1Entry(dir));
            }
            if (Directory.Exists(_paths.DvdFolder))
            {
                foreach (var file in Directory.GetFiles(_paths.DvdFolder, "*.ISO"))
                    Ps2Games.Add(new GameEntry { Name = Path.GetFileNameWithoutExtension(file), FolderPath = file, GameId = ExtractGameId(file) });
            }
            if (Directory.Exists(_paths.AppsFolder))
            {
                foreach (var file in Directory.GetFiles(_paths.AppsFolder, "*.ELF"))
                    AppsGames.Add(new GameEntry { Name = Path.GetFileNameWithoutExtension(file), FolderPath = file, GameId = ExtractGameId(file) });
            }
            Status = $"Juegos: {Ps1Games.Count} PS1, {Ps2Games.Count} PS2, {AppsGames.Count} APPs";
        }
        catch (Exception ex) { Status = $"Error al listar: {ex.Message}"; }
    }

    private GameEntry BuildPs1Entry(string discFolder)
    {
        string folderName = Path.GetFileName(discFolder);
        bool discs = Directory.Exists(Path.GetDirectoryName(discFolder))
                     && Directory.GetFiles(Path.GetDirectoryName(discFolder)!, "DISCS.TXT").Any();
        int discNumber = 1;
        if (discs)
        {
            var name = folderName.ToUpper();
            if (name.Contains("CD2") || name.Contains("DISC2")) discNumber = 2;
            else if (name.Contains("CD3") || name.Contains("DISC3")) discNumber = 3;
            else if (name.Contains("CD4") || name.Contains("DISC4")) discNumber = 4;
        }
        string vcd = Directory.GetFiles(discFolder, "*.VCD").FirstOrDefault() ?? "";
        string gameId = ExtractGameId(discFolder);
        string cleanTitle = NormalizeGameTitle(folderName, discNumber, true);

        return new GameEntry
        {
            Name = cleanTitle,
            FolderPath = discFolder,
            VcdPath = vcd,
            GameId = gameId,
            IsMultiDisc = discs,
            DiscNumber = discNumber
        };
    }

    private string NormalizeGameTitle(string rawName, int discNumber, bool includeDiscIfMulti)
    {
        string title = rawName;
        int dashIndex = title.IndexOf(" - ");
        if (dashIndex > 0) title = title.Substring(dashIndex + 3).Trim();
        else
        {
            var parts = title.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1 && parts[0].Length >= 4 && parts[0].Contains("-"))
                title = parts[1];
        }
        if (includeDiscIfMulti && discNumber > 1)
            title += $" (CD{discNumber})";
        return title.Trim();
    }

    private string ExtractGameId(string path)
    {
        try
        {
            var id = GameIdDetector.DetectGameId(path);
            if (!string.IsNullOrWhiteSpace(id)) return id;
        }
        catch { }
        string name = Path.GetFileNameWithoutExtension(path);
        return GameIdDetector.DetectFromName(name);
    }

    private async Task ProcessAllGames()
    {
        if (!Ps1Games.Any() && !Ps2Games.Any())
        {
            Status = "No hay juegos para procesar. Revisa la carpeta POPS/DVD.";
            return;
        }
        await GenerateAllElfs();
        await GenerateAllCheats();
        await DownloadAllCoversAndMetadata();
        Status = "Procesamiento completo.";
    }

    private async Task GenerateAllElfs()
    {
        Status = "Generando ELFs...";
        string baseElf = _paths.PopstarterElfPath;
        if (!File.Exists(baseElf))
        {
            Status = $"POPSTARTER.ELF no encontrado en {baseElf}. Cópialo a la raíz de la carpeta destino.";
            return;
        }

        int count = 0;
        foreach (var game in Ps1Games)
        {
            if (string.IsNullOrEmpty(game.VcdPath))
            {
                _log.Log($"[ELF] No se encontró VCD en {game.FolderPath}");
                continue;
            }
            ElfGenerator.GeneratePs1Elf(
                baseElf,
                game.VcdPath,
                _paths.AppsFolder,
                game.DiscNumber,
                game.Name,
                game.GameId,
                msg => _log.Log(msg));
            count++;
        }
        Status = count > 0 ? $"{count} ELFs generados." : "No se generaron ELFs (sin VCDs).";
    }

    private async Task GenerateAllCheats()
    {
        Status = "Generando cheats...";
        var extraLines = new List<string>();
        if (_cheatWidescreen) extraLines.Add("WIDESCREEN=ON");
        if (_cheatNoPal) extraLines.Add("$NOPAL");           // antes era FORCEVIDEO=1
        if (_cheatFixSound) extraLines.Add("FIXSOUND=ON");
        if (_cheatFixGraphics) extraLines.Add("FIXGRAPHICS=ON");

        int count = 0;
        foreach (var game in Ps1Games)
        {
            CheatGenerator.GenerateCheatTxt(game.GameId, game.FolderPath, extraLines, msg => _log.Log(msg));
            count++;
        }
        Status = count > 0 ? $"{count} CHEAT.TXT generados." : "No hay juegos de PS1 para cheats.";
    }

    private async Task DownloadAllCoversAndMetadata()
    {
        Status = "Descargando covers y metadatos...";
        int metaCount = 0;
        foreach (var game in Ps1Games.Concat(Ps2Games))
        {
            // Covers (placeholder hasta que haya URLs)
            string? coverUrl = null;
            if (coverUrl != null)
                await ArtDownloader.DownloadArtAsync(game.GameId, coverUrl, _paths.ArtFolder, msg => _log.Log(msg));
            else
                _log.Log($"[COVER] Sin URL para {game.GameId}");

            // Metadatos
            await MetadataDownloader.DownloadMetadataAsync(game.GameId, game.Name, _paths.CfgFolder, msg => _log.Log(msg));
            metaCount++;
        }
        Status = $"Covers y {metaCount} metadatos actualizados.";
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}