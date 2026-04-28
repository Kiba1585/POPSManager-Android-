using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using POPSManager.Core.Services;

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
    private string _elfFolderPath = string.Empty;
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
        ChangeElfFolderCommand = new Command(async () => await SafeExecute(ChangeElfFolder));
        OpenConvertCommand = new Command(async () => await SafeExecute(OpenConvert));
        OpenProcessPopsCommand = new Command(async () => await SafeExecute(OpenProcessPops));
        OpenRootFolderCommand = new Command(OpenRootFolder);
        OpenElfFolderCommand = new Command(OpenElfFolder);

        LoadData();
    }

    #region Properties

    public string SourcePath
    {
        get => _sourcePath;
        set => SetProperty(ref _sourcePath, value);
    }

    public string DestinationPath
    {
        get => _destinationPath;
        set => SetProperty(ref _destinationPath, value);
    }

    public string ElfFolderPath
    {
        get => _elfFolderPath;
        set => SetProperty(ref _elfFolderPath, value);
    }

    public bool ProcessSubfolders
    {
        get => _processSubfolders;
        set => SetProperty(ref _processSubfolders, value);
    }

    public string SystemInfo
    {
        get => _systemInfo;
        set => SetProperty(ref _systemInfo, value);
    }

    #endregion

    #region Commands

    public ICommand ChangeSourceFolderCommand { get; }
    public ICommand ChangeDestinationFolderCommand { get; }
    public ICommand ChangeElfFolderCommand { get; }
    public ICommand OpenConvertCommand { get; }
    public ICommand OpenProcessPopsCommand { get; }
    public ICommand OpenRootFolderCommand { get; }
    public ICommand OpenElfFolderCommand { get; }

    #endregion

    #region Methods

    private void LoadData()
    {
        SourcePath = _settings.SourceFolder ?? string.Empty;
        DestinationPath = _settings.DestinationFolder ?? string.Empty;
        ElfFolderPath = _settings.ElfFolder ?? string.Empty;

        SystemInfo =
            $"Versión: 1.0.0 Android\n" +
            $"Directorio base: {FileSystem.AppDataDirectory}\n" +
            $"Raíz: {_paths.RootFolder}";
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

        await _settings.SaveAsync();
    }

    private async Task ChangeElfFolder()
    {
        var path = await _paths.SelectFolderAsync();
        if (path == null) return;

        _settings.ElfFolder = path;
        ElfFolderPath = path;

        await _settings.SaveAsync();
    }

    private Task OpenConvert()
    {
        _log.Log("Abrir Convert (pendiente)");
        return Task.CompletedTask;
    }

    private Task OpenProcessPops()
    {
        _log.Log("Abrir Process POPS (pendiente)");
        return Task.CompletedTask;
    }

    private void OpenRootFolder()
    {
        _log.Log($"Abrir carpeta root: {_paths.RootFolder}");
    }

    private void OpenElfFolder()
    {
        _log.Log($"Abrir carpeta ELF: {ElfFolderPath}");
    }

    #endregion

    #region Helpers

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

    #endregion
}