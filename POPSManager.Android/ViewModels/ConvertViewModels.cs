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

    public ConvertViewModel(IPathsService paths, ConverterService converter, ILoggingService log)
    {
        _paths = paths;
        _converter = converter;
        _log = log;

        SelectSourceCommand = new Command(async () => await SafeExecute(SelectSource));
        SelectDestCommand = new Command(async () => await SafeExecute(SelectDest));
        ConvertCommand = new Command(async () => await SafeExecute(ConvertFiles));
        OpenOutputFolderCommand = new Command(OpenOutputFolder);
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

    private bool TienePermisoAllFiles()
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
        {
            return Android.OS.Environment.IsExternalStorageManager;
        }
#endif
        return true;
    }

    private async Task ConvertFiles()
    {
        if (string.IsNullOrEmpty(SourceFolder) || string.IsNullOrEmpty(DestFolder))
        {
            Status = "Selecciona origen y destino primero.";
            return;
        }

        // Verificar si tenemos permiso total de acceso a archivos
        if (!TienePermisoAllFiles())
        {
#if ANDROID
            Status = "Se necesita permiso 'Todos los archivos'. Actívalo en Ajustes > Aplicaciones > POPSManager.";
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionManageAllFilesAccessPermission);
            Android.App.Application.Context.StartActivity(intent);
#endif
            return;
        }

        string finalDest = DestFolder;

        // Verificar si la carpeta destino es escribible
        bool puedeEscribir = false;
        try
        {
            var testFile = Path.Combine(DestFolder, ".writetest");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            puedeEscribir = true;
        }
        catch { }

        if (!puedeEscribir)
        {
            // Fallback a carpeta interna
            finalDest = Path.Combine(FileSystem.AppDataDirectory, "Converted");
            Status = "No se pudo escribir en la carpeta seleccionada. Se usará almacenamiento interno.";
        }
        else
        {
            Status = "Convirtiendo...";
        }

        _actualOutputFolder = finalDest;
        Directory.CreateDirectory(finalDest);

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

    private void OpenOutputFolder()
    {
        if (string.IsNullOrEmpty(_actualOutputFolder))
        {
            Status = "Primero realiza una conversión.";
            return;
        }

        try
        {
#if ANDROID
            var context = Android.App.Application.Context;
            var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
            var uri = Android.Net.Uri.Parse(_actualOutputFolder);
            intent.SetDataAndType(uri, "resource/folder");
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
#endif
        }
        catch (Exception ex)
        {
            Status = $"No se pudo abrir la carpeta: {ex.Message}";
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