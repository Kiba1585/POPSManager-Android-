using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using POPSManager.Core.Services;

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

        // Cargar rutas guardadas desde Home
        LoadSavedPaths();
    }

    private void LoadSavedPaths()
    {
        var savedSource = _settings.SourceFolder;
        if (!string.IsNullOrEmpty(savedSource))
        {
            _sourceFolder = savedSource;
            OnPropertyChanged(nameof(SourceFolder));
            LoadFiles();
        }

        var savedDest = _settings.DestinationFolder;
        if (!string.IsNullOrEmpty(savedDest))
        {
            _destFolder = savedDest;
            OnPropertyChanged(nameof(DestFolder));
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
            await _settings.SaveAsync();
            DestFolder = path;
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

        // Configurar el converter para que reporte progreso
        var progressConverter = new ConverterService(
            log: msg => _log.Log(msg),
            setStatus: msg => MainThread.BeginInvokeOnMainThread(() => Status = msg)
        );

        _actualOutputFolder = DestFolder;

        Status = "Convirtiendo...";
        try
        {
            await progressConverter.ConvertFolderAsync(SourceFolder, DestFolder);
            Status = $"Conversión completada. Archivos en: {DestFolder}";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
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
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }
}