using System.Collections.ObjectModel;
using System.Windows.Input;
using POPSManager.Core.Services;

namespace POPSManager.Android.ViewModels;

public class LogEntry
{
    public string Message { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
}

public class HomeViewModel : BindableObject
{
    private readonly IPathsService _paths;
    private readonly ILoggingService _log;
    private readonly ConverterService _converter;
    private readonly SettingsService _settings;

    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    private string _sourcePath = "";
    private string _destinationPath = "";
    private string _elfFolderPath = "";
    private bool _processSubfolders = true;
    private string _systemInfo = "";

    public HomeViewModel(IPathsService paths, ILoggingService log, ConverterService converter, SettingsService settings)
    {
        _paths = paths;
        _log = log;
        _converter = converter;
        _settings = settings;

        ChangeSourceFolderCommand = new Command(async () => await ChangeSourceFolder());
        ChangeDestinationFolderCommand = new Command(async () => await ChangeDestinationFolder());
        ChangeElfFolderCommand = new Command(async () => await ChangeElfFolder());
        OpenConvertCommand = new Command(OpenConvert);
        OpenProcessPopsCommand = new Command(OpenProcessPops);
        OpenRootFolderCommand = new Command(OpenRootFolder);
        OpenElfFolderCommand = new Command(OpenElfFolder);

        LoadData();
    }

    public string SourcePath { get => _sourcePath; set { _sourcePath = value; OnPropertyChanged(); } }
    public string DestinationPath { get => _destinationPath; set { _destinationPath = value; OnPropertyChanged(); } }
    public string ElfFolderPath { get => _elfFolderPath; set { _elfFolderPath = value; OnPropertyChanged(); } }
    public bool ProcessSubfolders { get => _processSubfolders; set { _processSubfolders = value; OnPropertyChanged(); } }
    public string SystemInfo { get => _systemInfo; set { _systemInfo = value; OnPropertyChanged(); } }

    public ICommand ChangeSourceFolderCommand { get; }
    public ICommand ChangeDestinationFolderCommand { get; }
    public ICommand ChangeElfFolderCommand { get; }
    public ICommand OpenConvertCommand { get; }
    public ICommand OpenProcessPopsCommand { get; }
    public ICommand OpenRootFolderCommand { get; }
    public ICommand OpenElfFolderCommand { get; }

    private void LoadData()
    {
        SourcePath = _settings.SourceFolder ?? "";
        DestinationPath = _settings.DestinationFolder ?? "";
        ElfFolderPath = _settings.ElfFolder ?? "";
        SystemInfo = $"Versión: 1.0.0 Android\nDirectorio base: {FileSystem.AppDataDirectory}\nRaíz: {_paths.RootFolder}";
    }

    private async Task ChangeSourceFolder()
    {
        var path = await _paths.SelectFolderAsync();
        if (path != null)
        {
            _settings.SourceFolder = path;
            SourcePath = path;
            await _settings.SaveAsync();
        }
    }

    private async Task ChangeDestinationFolder()
    {
        var path = await _paths.SelectFolderAsync();
        if (path != null)
        {
            _settings.DestinationFolder = path;
            _settings.RootFolder = path;
            DestinationPath = path;
            await _settings.SaveAsync();
        }
    }

    private async Task ChangeElfFolder()
    {
        var path = await _paths.SelectFolderAsync();
        if (path != null)
        {
            _settings.ElfFolder = path;
            ElfFolderPath = path;
            await _settings.SaveAsync();
        }
    }

    private void OpenConvert() { }
    private void OpenProcessPops() { }
    private void OpenRootFolder() { }
    private void OpenElfFolder() { }
}