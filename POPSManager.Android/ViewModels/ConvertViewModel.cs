using System.Collections.ObjectModel;
using System.Windows.Input;
using POPSManager.Core.Services;

namespace POPSManager.Android.ViewModels;

public class FileItem
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
}

public class ConvertViewModel : ObservableObject
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

    public string SourceFolder { get => _sourceFolder; set { SetProperty(ref _sourceFolder, value); LoadFiles(); } }
    public string DestFolder { get => _destFolder; set => SetProperty(ref _destFolder, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public ConvertViewModel(IPathsService paths, ConverterService converter, ILoggingService log)
    {
        _paths = paths;
        _converter = converter;
        _log = log;

        SelectSourceCommand = new Command(async () => await SelectSource());
        SelectDestCommand = new Command(async () => await SelectDest());
        ConvertCommand = new Command(async () => await ConvertFiles());
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
        if (!Directory.Exists(SourceFolder)) return;

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

    private async Task ConvertFiles()
    {
        Status = "Convirtiendo...";
        await _converter.ConvertFolderAsync(SourceFolder, DestFolder);
        Status = "Conversión completada.";
    }
}