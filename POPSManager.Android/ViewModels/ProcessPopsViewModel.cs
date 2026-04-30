using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using POPSManager.Core.Services;
using POPSManager.Android.Services;

namespace POPSManager.Android.ViewModels;

public class ProcessPopsViewModel : BindableObject
{
    private readonly IPathsService _paths;
    private readonly GameProcessor _processor;
    private readonly SettingsService _settings;

    private string _status = "";
    private bool _canProcess;
    private string? _selectedFolder;

    public ObservableCollection<string> Ps1Games { get; } = new();
    public ObservableCollection<string> Ps2Games { get; } = new();
    public ObservableCollection<string> AppsGames { get; } = new();

    public ICommand SelectFolderCommand { get; }
    public ICommand ProcessCommand { get; }

    public string Status
    {
        get => _status;
        set { if (_status != value) { _status = value; OnPropertyChanged(); } }
    }
    public bool CanProcess
    {
        get => _canProcess;
        set { if (_canProcess != value) { _canProcess = value; OnPropertyChanged(); } }
    }

    public ProcessPopsViewModel(IPathsService paths, GameProcessor processor, SettingsService settings)
    {
        _paths = paths;
        _processor = processor;
        _settings = settings;

        SelectFolderCommand = new Command(async () => await SelectFolder());
        ProcessCommand = new Command(async () => await ProcessGames());

        // Cargar automáticamente la carpeta de destino guardada en Inicio
        LoadDestinationFolder();
    }

    private void LoadDestinationFolder()
    {
        var destFolder = _settings.DestinationFolder;
        if (!string.IsNullOrEmpty(destFolder))
        {
            _selectedFolder = destFolder;
            CanProcess = true;
            Status = $"Carpeta de destino (desde Inicio): {destFolder}";

            // Si la implementación es Android, actualizar RootFolder
            if (_paths is PathsServiceAndroid androidPaths)
            {
                androidPaths.RootFolder = destFolder;
            }
        }
        else
        {
            Status = "No hay carpeta de destino configurada en Inicio.";
        }
    }

    private async Task SelectFolder()
    {
        try
        {
            _selectedFolder = await _paths.SelectFolderAsync();
            CanProcess = !string.IsNullOrEmpty(_selectedFolder);
            Status = _selectedFolder ?? "No se seleccionó carpeta.";

            // Actualizar también la raíz OPL
            if (!string.IsNullOrEmpty(_selectedFolder) && _paths is PathsServiceAndroid androidPaths)
            {
                androidPaths.RootFolder = _selectedFolder;
            }
        }
        catch (Exception ex)
        {
            Status = $"Error al seleccionar carpeta: {ex.Message}";
        }
    }

    private async Task ProcessGames()
    {
        if (_selectedFolder == null) return;

        Status = "Procesando...";
        try
        {
            await _processor.ProcessFolderAsync(_selectedFolder);
            Status = "Procesamiento completado.";

            // Escanear las carpetas de destino y llenar las listas
            RefreshGameLists();
        }
        catch (Exception ex)
        {
            Status = $"Error durante el procesamiento: {ex.Message}";
        }
    }

    private void RefreshGameLists()
    {
        Ps1Games.Clear();
        Ps2Games.Clear();
        AppsGames.Clear();

        try
        {
            // PS1 (POPS)
            if (Directory.Exists(_paths.PopsFolder))
            {
                foreach (var dir in Directory.GetDirectories(_paths.PopsFolder))
                    Ps1Games.Add(Path.GetFileName(dir));
            }

            // PS2 (DVD)
            if (Directory.Exists(_paths.DvdFolder))
            {
                foreach (var file in Directory.GetFiles(_paths.DvdFolder, "*.ISO"))
                    Ps2Games.Add(Path.GetFileNameWithoutExtension(file));
            }

            // APPS (si aplica)
            if (Directory.Exists(_paths.AppsFolder))
            {
                foreach (var file in Directory.GetFiles(_paths.AppsFolder, "*.ELF"))
                    AppsGames.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
        catch (Exception ex)
        {
            Status += $"\nError al listar juegos: {ex.Message}";
        }
    }
}