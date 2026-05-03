using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using POPSManager.Core.Logic;
using POPSManager.Core.Services;
using POPSManager.Android.Models;
using POPSManager.Android.Services;

namespace POPSManager.Android.ViewModels;

public class ProcessPopsViewModel : BindableObject
{
    private readonly IPathsService _paths;
    private readonly SettingsService _settings;
    private readonly GameListService _listService;
    private readonly GameProcessingService _processingService;
    private readonly GameAssetService _assetService;

    public ObservableCollection<GameItem> Ps1Games => _listService.Ps1Games;
    public ObservableCollection<GameItem> Ps2Games => _listService.Ps2Games;
    public ObservableCollection<GameItem> AppsGames => _listService.AppsGames;

    private bool _cheatWidescreen, _cheatNoPal, _cheatFixSound, _cheatFixGraphics;
    public bool CheatWidescreen { get => _cheatWidescreen; set => SetProperty(ref _cheatWidescreen, value); }
    public bool CheatNoPal { get => _cheatNoPal; set => SetProperty(ref _cheatNoPal, value); }
    public bool CheatFixSound { get => _cheatFixSound; set => SetProperty(ref _cheatFixSound, value); }
    public bool CheatFixGraphics { get => _cheatFixGraphics; set => SetProperty(ref _cheatFixGraphics, value); }

    private bool _isUpdateModeIndividual = true;
    public bool IsUpdateModeIndividual
    {
        get => _isUpdateModeIndividual;
        set => SetProperty(ref _isUpdateModeIndividual, value);
    }

    private string _oplRootFolder = "";
    public string OplRootFolder
    {
        get => _oplRootFolder;
        set
        {
            string sanitized = value?.Trim() ?? "";
            if (_oplRootFolder != sanitized)
            {
                _oplRootFolder = sanitized;
                if (_paths is PathsServiceAndroid androidPaths) androidPaths.RootFolder = sanitized;
                OnPropertyChanged();
            }
        }
    }

    private string _status = "";
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public ICommand SelectOplRootFolderCommand { get; }
    public ICommand ProcessAllCommand { get; }
    public ICommand GenerateElfCommand { get; }
    public ICommand GenerateCheatsCommand { get; }
    public ICommand DownloadCoversCommand { get; }
    public ICommand CopyMetadataCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand RenameAllCommand { get; }
    public ICommand OpenStorageSettingsCommand { get; }
    public ICommand UpdateDatabaseCommand { get; }

    public ProcessPopsViewModel(IPathsService paths, SettingsService settings,
        GameListService listService, GameProcessingService processingService, GameAssetService assetService)
    {
        _paths = paths;
        _settings = settings;
        _listService = listService;
        _processingService = processingService;
        _assetService = assetService;

        SelectOplRootFolderCommand = new Command(async () => await SelectOplRootFolder());
        ProcessAllCommand = new Command(async () => await ProcessAllGames());
        GenerateElfCommand = new Command(async () => await GenerateAllElfs());
        GenerateCheatsCommand = new Command(async () => await GenerateAllCheats());
        DownloadCoversCommand = new Command(async () => await DownloadCovers());
        CopyMetadataCommand = new Command(async () => await CopyMetadata());
        RefreshCommand = new Command(() => Status = _listService.Refresh());
        RenameAllCommand = new Command(async () => await RenameAllGames());
        OpenStorageSettingsCommand = new Command(OpenStorageSettings);
        UpdateDatabaseCommand = new Command(async () => await UpdateDatabase());

        RefreshFromSettings();
    }

    private void ReportProgress(string msg) =>
        MainThread.BeginInvokeOnMainThread(() => Status = msg);

    public void RefreshFromSettings()
    {
        var savedRoot = _settings.DestinationFolder;
        if (!string.IsNullOrEmpty(savedRoot))
        {
            OplRootFolder = savedRoot;
            Status = _listService.Refresh();
            GameDatabase.Initialize(DatabaseUpdaterService.InternalDatabaseFolder);
        }
        else Status = "Selecciona la carpeta raíz OPL.";
    }

    private async Task SelectOplRootFolder()
    {
        var path = await _paths.SelectFolderAsync();
        if (path != null)
        {
            _settings.DestinationFolder = path;
            _settings.RootFolder = path;
            await _settings.SaveAsync();
            OplRootFolder = path;
            Status = _listService.Refresh();
            GameDatabase.Initialize(DatabaseUpdaterService.InternalDatabaseFolder);
        }
    }

    private async Task UpdateDatabase()
    {
        if (IsUpdateModeIndividual)
            Status = await _assetService.UpdateIndividualAsync(ReportProgress);
        else
            Status = await _assetService.CheckAndUpdateFullAsync(ReportProgress);
    }

    private async Task DownloadCovers() =>
        Status = await _assetService.DownloadCoversAsync(ReportProgress);

    private async Task CopyMetadata() =>
        Status = await _assetService.CopyMetadataAsync(ReportProgress);

    private async Task ProcessAllGames()
    {
        if (!_listService.Ps1Games.Any() && !_listService.Ps2Games.Any()) { Status = "No hay juegos."; return; }
        Status = await _processingService.GenerateAllElfsAsync();
        Status = await _processingService.GenerateAllCheatsAsync(CheatWidescreen, CheatNoPal, CheatFixSound, CheatFixGraphics);
        await DownloadCovers();
        await CopyMetadata();
        Status = "Procesamiento completo.";
    }

    private async Task GenerateAllElfs() =>
        Status = await _processingService.GenerateAllElfsAsync();

    private async Task GenerateAllCheats() =>
        Status = await _processingService.GenerateAllCheatsAsync(CheatWidescreen, CheatNoPal, CheatFixSound, CheatFixGraphics);

    private async Task RenameAllGames() =>
        Status = await _processingService.RenameAllAsync(CheatWidescreen, CheatNoPal, CheatFixSound, CheatFixGraphics);

    private void OpenStorageSettings()
    {
        try
        {
            var intent = new global::Android.Content.Intent(global::Android.Provider.Settings.ActionManageAllFilesAccessPermission);
            global::Android.App.Application.Context.StartActivity(intent);
        }
        catch { }
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [System.Runtime.CompilerServices.CallerMemberName] string prop = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
        backingStore = value;
        OnPropertyChanged(prop);
        return true;
    }
}