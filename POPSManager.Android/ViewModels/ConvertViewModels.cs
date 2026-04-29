using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

    private string _sourceFolder = "";
    private string _destFolder = "";
    private string _status = "";

    public ObservableCollection<FileItem> Files { get; } = new();

    public ICommand SelectSourceCommand { get; }
    public ICommand SelectDestCommand { get; }
    public ICommand ConvertCommand { get; }

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

    public ConvertViewModel(IPathsService paths, ConverterService converter, ILoggingService log)
    {
        _paths = paths;
        _converter = converter;
        _log = log;

        SelectSourceCommand = new Command(async () => await SafeExecute(SelectSource));
        SelectDestCommand = new Command(async () => await SafeExecute(SelectDest));
        ConvertCommand = new Command(async () => await SafeExecute(ConvertFiles));
    }

    private async Task SelectSource()
    {
        var path = await _paths.SelectFolderAsync();
        if (path != null) SourceFolder = path;
    }

    private async Task SelectDest()
    {
        var path = await _paths.SelectFolderAsync();
        if (path != null) DestFolder = path;
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
                            f.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileItem
                {
                    Name = Path.GetFileName(f),
                    Icon = Path.GetExtension(f).ToLowerInvariant() switch
                    {
                        ".bin" => "💾",
                        ".cue" => "📄",
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

    // Si la carpeta de destino no es escribible (Android), usamos una interna
    string finalDest = DestFolder;
    try
    {
        // Intenta crear un archivo de prueba para ver si hay acceso
        var testPath = System.IO.Path.Combine(DestFolder, ".writetest");
        System.IO.File.WriteAllText(testPath, "test");
        System.IO.File.Delete(testPath);
    }
    catch
    {
        // Usar carpeta interna segura
        finalDest = System.IO.Path.Combine(FileSystem.AppDataDirectory, "Converted");
        Status = "Usando carpeta interna (acceso denegado a la seleccionada).";
    }

    Status = "Convirtiendo...";
    try
    {
        await _converter.ConvertFolderAsync(SourceFolder, finalDest);
        Status = $"Conversión completada. Archivos en: {finalDest}";
    }
    catch (Exception ex)
    {
        Status = $"Error: {ex.Message}";
    }
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