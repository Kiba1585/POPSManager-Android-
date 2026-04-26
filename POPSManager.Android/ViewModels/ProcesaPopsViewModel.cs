using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using POPSManager.Core.Services;

namespace POPSManager.Android.ViewModels;

public class ProcessPopsViewModel : ObservableObject
{
    private readonly IPathsService _paths;
    private readonly GameProcessor _processor;

    private string _status = "";
    private bool _canProcess;
    private string? _selectedFolder;

    public ObservableCollection<string> Ps1Games { get; } = new();
    public ObservableCollection<string> Ps2Games { get; } = new();
    public ObservableCollection<string> AppsGames { get; } = new();

    public ICommand SelectFolderCommand { get; }
    public ICommand ProcessCommand { get; }

    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public bool CanProcess { get => _canProcess; set => SetProperty(ref _canProcess, value); }

    public ProcessPopsViewModel(IPathsService paths, GameProcessor processor)
    {
        _paths = paths;
        _processor = processor;

        SelectFolderCommand = new Command(async () => await SelectFolder());
        ProcessCommand = new Command(async () => await ProcessGames());
    }

    private async Task SelectFolder()
    {
        _selectedFolder = await _paths.SelectFolderAsync();
        CanProcess = !string.IsNullOrEmpty(_selectedFolder);
        Status = _selectedFolder ?? "No se seleccionó carpeta.";
    }

    private async Task ProcessGames()
    {
        if (_selectedFolder == null) return;
        Status = "Procesando...";
        var games = await _processor.DetectGamesAsync(_selectedFolder);
        await _processor.ProcessGamesAsync(_selectedFolder, games, _paths.PopsFolder);
        Status = "Procesamiento completado.";
    }
}