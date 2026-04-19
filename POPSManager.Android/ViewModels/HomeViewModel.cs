using System.Collections.ObjectModel;
using System.Windows.Input;
using POPSManager.Core.Models;
using POPSManager.Core.Services;

namespace POPSManager.Android.ViewModels;

public class HomeViewModel : BindableObject
{
    private readonly IPathsService _paths;
    private readonly GameProcessor _processor;

    public ObservableCollection<GameProgressItem> Games { get; } = new();

    public ICommand SelectFolderCommand { get; }
    public ICommand ProcessGamesCommand { get; }

    private string? _selectedFolder;
    public bool CanProcess => !string.IsNullOrEmpty(_selectedFolder);

    private string _statusMessage = "Seleccione una carpeta para comenzar.";
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public HomeViewModel(IPathsService paths, GameProcessor processor)
    {
        _paths = paths;
        _processor = processor;

        SelectFolderCommand = new Command(async () => await SelectFolder());
        ProcessGamesCommand = new Command(async () => await ProcessGames());
    }

    private async Task SelectFolder()
    {
        _selectedFolder = await _paths.SelectFolderAsync();
        OnPropertyChanged(nameof(CanProcess));

        StatusMessage = _selectedFolder is null
            ? "No se seleccionó carpeta."
            : $"Carpeta seleccionada:\n{_selectedFolder}";
    }

    private async Task ProcessGames()
    {
        if (_selectedFolder is null)
            return;

        Games.Clear();
        StatusMessage = "Detectando juegos...";

        var detected = await _processor.DetectGamesAsync(_selectedFolder);

        foreach (var g in detected)
            Games.Add(g);

        StatusMessage = "Procesando juegos...";

        await _processor.ProcessGamesAsync(_selectedFolder, Games);

        StatusMessage = "Completado.";
    }
}