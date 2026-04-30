using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using POPSManager.Core.Services;
using POPSManager.Android.Services;

namespace POPSManager.Android.ViewModels;

public class LogEntry
{
    public string Message { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class HomeViewModel : BindableObject
{
    private readonly IPathsService _paths;
    private readonly ILoggingService _log;
    private readonly ConverterService _converter;
    private readonly SettingsService _settings;

    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    private string _sourcePath = string.Empty;
    private string _destinationPath = string.Empty;
    private bool _processSubfolders = true;
    private string _systemInfo = string.Empty;

    public HomeViewModel(
        IPathsService paths,
        ILoggingService log,
        ConverterService converter,
        SettingsService settings)
    {
        _paths = paths;
        _log = log;
        _converter = converter;
        _settings = settings;

        ChangeSourceFolderCommand = new Command(async () => await SafeExecute(ChangeSourceFolder));
        ChangeDestinationFolderCommand = new Command(async () => await SafeExecute(ChangeDestinationFolder));
        OpenConvertCommand = new Command(async () => await NavegarA("Convertir"));
        OpenProcessPopsCommand = new Command(async () => await NavegarA("Procesar"));
        OpenRootFolderCommand = new Command(OpenRootFolder);

        LoadData();
    }

    public string SourcePath { get => _sourcePath; set => SetProperty(ref _sourcePath, value); }
    public string DestinationPath { get => _destinationPath; set => SetProperty(ref _destinationPath, value); }
    public bool ProcessSubfolders { get => _processSubfolders; set => SetProperty(ref _processSubfolders, value); }
    public string SystemInfo { get => _systemInfo; set => SetProperty(ref _systemInfo, value); }

    public ICommand ChangeSourceFolderCommand { get; }
    public ICommand ChangeDestinationFolderCommand { get; }
    public ICommand OpenConvertCommand { get; }
    public ICommand OpenProcessPopsCommand { get; }
    public ICommand OpenRootFolderCommand { get; }

    private void LoadData()
    {
        SourcePath = _settings.SourceFolder ?? string.Empty;
        DestinationPath = _settings.DestinationFolder ?? string.Empty;

        SystemInfo =
            $"Versión: 1.0.0 Android\n" +
            $"Directorio base: {FileSystem.AppDataDirectory}\n" +
            $"Raíz OPL: {_paths.RootFolder}";
    }

    private async Task ChangeSourceFolder()
    {
        var path = await _paths.SelectFolderAsync();
        if (path == null) return;

        _settings.SourceFolder = path;
        SourcePath = path;
        await _settings.SaveAsync();
    }

    private async Task ChangeDestinationFolder()
    {
        var path = await _paths.SelectFolderAsync();
        if (path == null) return;

        _settings.DestinationFolder = path;
        _settings.RootFolder = path;
        DestinationPath = path;

        // Actualizar raíz OPL y crear estructura
        if (_paths is PathsServiceAndroid androidPaths)
        {
            androidPaths.RootFolder = path;
            // EnsureOplFoldersExist se llama automáticamente al asignar RootFolder
        }

        await _settings.SaveAsync();
    }

    private async Task NavegarA(string ruta)
    {
        await Shell.Current.GoToAsync($"//{ruta}");
    }

    private void OpenRootFolder()
    {
        if (!string.IsNullOrEmpty(DestinationPath))
            _paths.OpenFolder(DestinationPath);
    }

    private async Task SafeExecute(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _log.Log($"Error: {ex.Message}");
            LogEntries.Add(new LogEntry
            {
                Message = ex.Message,
                Color = "Red"
            });
        }
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}