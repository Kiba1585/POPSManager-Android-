using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using POPSManager.Core.Services;
using POPSManager.Android.Services;

namespace POPSManager.Android.ViewModels;

public class ProcessPopsViewModel : BindableObject
{
    private readonly IPathsService _paths;
    private readonly GameProcessor _processor;
    private readonly SettingsService _settings;

    private string _oplRootFolder = "";
    private string _status = "";

    public ObservableCollection<string> Ps1Games { get; } = new();
    public ObservableCollection<string> Ps2Games { get; } = new();
    public ObservableCollection<string> AppsGames { get; } = new();

    public ICommand SelectOplRootFolderCommand { get; }
    public ICommand ProcessAllCommand { get; }
    public ICommand GenerateElfCommand { get; }
    public ICommand GenerateCheatsCommand { get; }
    public ICommand DownloadCoversCommand { get; }
    public ICommand RefreshCommand { get; }

    public string OplRootFolder
    {
        get => _oplRootFolder;
        set { if (_oplRootFolder != value) { _oplRootFolder = value; OnPropertyChanged(); } }
    }
    public string Status
    {
        get => _status;
        set { if (_status != value) { _status = value; OnPropertyChanged(); } }
    }

    public ProcessPopsViewModel(IPathsService paths, GameProcessor processor, SettingsService settings)
    {
        _paths = paths;
        _processor = processor;
        _settings = settings;

        SelectOplRootFolderCommand = new Command(async () => await SelectOplRootFolder());
        ProcessAllCommand = new Command(async () => await ProcessAllGames());
        GenerateElfCommand = new Command(async () => await GenerateAllElfs());
        GenerateCheatsCommand = new Command(async () => await GenerateAllCheats());
        DownloadCoversCommand = new Command(async () => await DownloadAllCovers());
        RefreshCommand = new Command(RefreshGameLists);

        // Cargar carpeta raíz OPL desde los ajustes guardados en Inicio
        LoadOplRootFolder();
    }

    private void LoadOplRootFolder()
    {
        var savedRoot = _settings.DestinationFolder;
        if (!string.IsNullOrEmpty(savedRoot))
        {
            _oplRootFolder = savedRoot;
            if (_paths is PathsServiceAndroid androidPaths)
                androidPaths.RootFolder = savedRoot;
            Status = $"Raíz OPL: {savedRoot}";
            RefreshGameLists();
        }
        else
        {
            Status = "Selecciona la carpeta raíz OPL (desde Inicio o aquí).";
        }
    }

    private async Task SelectOplRootFolder()
    {
        try
        {
            var path = await _paths.SelectFolderAsync();
            if (path != null)
            {
                _settings.DestinationFolder = path;
                _settings.RootFolder = path;
                await _settings.SaveAsync();
                OplRootFolder = path;

                if (_paths is PathsServiceAndroid androidPaths)
                    androidPaths.RootFolder = path;

                RefreshGameLists();
            }
        }
        catch (Exception ex)
        {
            Status = $"Error al seleccionar raíz OPL: {ex.Message}";
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
                    Ps1Games.Add(Path.GetFileName(dir));
            }
            if (Directory.Exists(_paths.DvdFolder))
            {
                foreach (var file in Directory.GetFiles(_paths.DvdFolder, "*.ISO"))
                    Ps2Games.Add(Path.GetFileNameWithoutExtension(file));
            }
            if (Directory.Exists(_paths.AppsFolder))
            {
                foreach (var file in Directory.GetFiles(_paths.AppsFolder, "*.ELF"))
                    AppsGames.Add(Path.GetFileNameWithoutExtension(file));
            }
            Status = $"Juegos cargados: {Ps1Games.Count} PS1, {Ps2Games.Count} PS2, {AppsGames.Count} APPs";
        }
        catch (Exception ex)
        {
            Status = $"Error al listar juegos: {ex.Message}";
        }
    }

    private async Task ProcessAllGames()
    {
        Status = "Procesando todos los juegos...";
        try
        {
            await GenerateAllElfs();
            await GenerateAllCheats();
            await DownloadAllCovers();
            Status = "Procesamiento completo.";
        }
        catch (Exception ex)
        {
            Status = $"Error durante el procesamiento: {ex.Message}";
        }
    }

    private async Task GenerateAllElfs()
    {
        Status = "Generando ELFs...";
        foreach (var game in Ps1Games)
        {
            string gamePath = Path.Combine(_paths.PopsFolder, game);
            await _processor.GenerateElfAsync(gamePath, game, "PS1");
        }
        Status += " OK";
    }

    private async Task GenerateAllCheats()
    {
        Status = "Generando cheats...";
        foreach (var game in Ps1Games)
        {
            string gamePath = Path.Combine(_paths.PopsFolder, game);
            await _processor.GenerateCheatsAsync(game, gamePath, "PS1");
        }
        foreach (var game in Ps2Games)
        {
            string gamePath = Path.Combine(_paths.DvdFolder, game + ".ISO");
            await _processor.GenerateCheatsAsync(game, gamePath, "PS2");
        }
        Status += " OK";
    }

    private async Task DownloadAllCovers()
    {
        Status = "Descargando covers...";
        foreach (var game in Ps1Games)
            await _processor.DownloadCoverAsync(game, "PS1");
        foreach (var game in Ps2Games)
            await _processor.DownloadCoverAsync(game, "PS2");
        Status += " OK";
    }
}