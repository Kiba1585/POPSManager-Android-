using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using POPSManager.Core.Logic;
using POPSManager.Core.Services;
using POPSManager.Android.Services;

namespace POPSManager.Android.ViewModels;

public class FileItem
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
}

public class ConvertViewModel : BindableObject
{
    private readonly IPathsService _paths;
    private readonly ConverterService _converter;
    private readonly ILoggingService _log;
    private readonly SettingsService _settings;

    private string _sourceFolder = "";
    private string _destFolder = "";
    private string _status = "";
    private string _actualOutputFolder = "";

    public ObservableCollection<FileItem> Files { get; } = new();

    public ICommand SelectSourceCommand { get; }
    public ICommand SelectDestCommand { get; }
    public ICommand ConvertCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }

    public string SourceFolder
    {
        get => _sourceFolder;
        set { if (_sourceFolder != value) { _sourceFolder = value; OnPropertyChanged(); LoadFiles(); } }
    }
    public string DestFolder
    {
        get => _destFolder;
        set { if (_destFolder != value) { _destFolder = value; OnPropertyChanged(); } }
    }
    public string Status
    {
        get => _status;
        set { if (_status != value) { _status = value; OnPropertyChanged(); } }
    }

    public ConvertViewModel(IPathsService paths, ConverterService converter, ILoggingService log, SettingsService settings)
    {
        _paths = paths;
        _converter = converter;
        _log = log;
        _settings = settings;

        SelectSourceCommand = new Command(async () => await SafeExecute(SelectSource));
        SelectDestCommand = new Command(async () => await SafeExecute(SelectDest));
        ConvertCommand = new Command(async () => await SafeExecute(ConvertFiles));
        OpenOutputFolderCommand = new Command(OpenOutputFolder);

        RefreshFromSettings();
    }

    public void RefreshFromSettings()
    {
        var savedSource = _settings.SourceFolder;
        if (!string.IsNullOrEmpty(savedSource) && savedSource != _sourceFolder)
        {
            _sourceFolder = savedSource;
            OnPropertyChanged(nameof(SourceFolder));
            LoadFiles();
        }

        var savedDest = _settings.DestinationFolder;
        if (!string.IsNullOrEmpty(savedDest) && savedDest != _destFolder)
        {
            _destFolder = savedDest;
            OnPropertyChanged(nameof(DestFolder));

            if (_paths is PathsServiceAndroid androidPaths)
            {
                androidPaths.RootFolder = savedDest;
                androidPaths.EnsureOplFoldersExist();
            }
        }
    }

    private async Task SelectSource()
    {
        var path = await _paths.SelectFolderAsync();
        if (path != null)
        {
            _settings.SourceFolder = path;
            await _settings.SaveAsync();
            SourceFolder = path;
        }
    }

    private async Task SelectDest()
    {
        var path = await _paths.SelectFolderAsync();
        if (path != null)
        {
            _settings.DestinationFolder = path;
            _settings.RootFolder = path;
            await _settings.SaveAsync();
            DestFolder = path;

            if (_paths is PathsServiceAndroid androidPaths)
            {
                androidPaths.RootFolder = path;
                androidPaths.EnsureOplFoldersExist();
            }
        }
    }

    private void LoadFiles()
    {
        Files.Clear();
        if (string.IsNullOrEmpty(SourceFolder) || !Directory.Exists(SourceFolder))
            return;

        try
        {
            var files = Directory.GetFiles(SourceFolder, "*.*")
                .Where(f => f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileItem
                {
                    Name = Path.GetFileName(f),
                    Icon = Path.GetExtension(f).ToLowerInvariant() switch
                    {
                        ".bin" => "💾",
                        ".iso" => "💿",
                        _ => "📁"
                    }
                })
                .ToList();

            foreach (var f in files) Files.Add(f);
            Status = $"{Files.Count} archivos listos para convertir.";
        }
        catch (Exception ex)
        {
            Status = $"Error al listar archivos: {ex.Message}";
        }
    }

    private async Task ConvertFiles()
    {
        if (string.IsNullOrEmpty(SourceFolder) || string.IsNullOrEmpty(DestFolder))
        {
            Status = "Selecciona origen y destino primero.";
            return;
        }

        string outputFolder = _paths.PopsFolder;
        try { Directory.CreateDirectory(outputFolder); }
        catch (Exception ex) { Status = $"Error al crear carpeta de salida: {ex.Message}"; return; }

        var progressConverter = new ConverterService(
            log: msg => _log.Log(msg),
            setStatus: msg => MainThread.BeginInvokeOnMainThread(() => Status = msg));

        _actualOutputFolder = outputFolder;
        Status = "Convirtiendo...";

        int converted = 0;
        var binFiles = Directory.GetFiles(SourceFolder, "*.bin", SearchOption.TopDirectoryOnly);

        foreach (var file in binFiles)
        {
            try
            {
                // Extraer Game ID del contenido del archivo .bin
                string gameId = ExtractGameId(file);
                if (string.IsNullOrWhiteSpace(gameId))
                {
                    _log.Log($"[Convertir] No se pudo extraer Game ID de {Path.GetFileName(file)}");
                    continue;
                }

                // Nombre limpio para el juego (sin ID, usando el nombre del archivo original)
                string rawName = Path.GetFileNameWithoutExtension(file);
                string cleanName = GameListService.OplCompatibleTitle(rawName, 1, false);

                // Construir nombre de salida: GAMEID.Nombre_Limpio.VCD
                string outputFileName = $"{gameId}.{cleanName}.VCD";
                string outputPath = Path.Combine(outputFolder, outputFileName);

                // Convertir el archivo
                await progressConverter.ConvertFileAsync(
                    file,
                    outputPath,
                    msg => _log.Log(msg),
                    msg => MainThread.BeginInvokeOnMainThread(() => Status = msg));

                converted++;
            }
            catch (Exception ex)
            {
                _log.Log($"ERROR convirtiendo {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        Status = converted > 0
            ? $"{converted} archivos convertidos en: {outputFolder}"
            : "No se encontraron archivos .bin para convertir.";
    }

    /// <summary> Extrae el Game ID del archivo (usa el contenido, no solo el nombre). </summary>
    private string ExtractGameId(string filePath)
    {
        try
        {
            var id = GameIdDetector.DetectGameId(filePath);
            if (!string.IsNullOrWhiteSpace(id)) return id;
        }
        catch { }
        // Fallback: extraer del nombre del archivo
        return GameIdDetector.DetectFromName(Path.GetFileNameWithoutExtension(filePath));
    }

    private void OpenOutputFolder()
    {
        if (!string.IsNullOrEmpty(_actualOutputFolder))
            _paths.OpenFolder(_actualOutputFolder);
        else
            Status = "Primero realiza una conversión.";
    }

    private async Task SafeExecute(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { Status = $"Error: {ex.Message}"; }
    }
}