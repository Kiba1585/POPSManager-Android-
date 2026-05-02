using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using POPSManager.Core.Logic;
using POPSManager.Core.Logic.Covers;
using POPSManager.Core.Services;
using POPSManager.Android.Services;

namespace POPSManager.Android.ViewModels;

public class ProcessPopsViewModel : BindableObject
{
    private readonly IPathsService _paths;
    private readonly ILoggingService _log;
    private readonly SettingsService _settings;

    public ObservableCollection<GameEntry> Ps1Games { get; } = new();
    public ObservableCollection<GameEntry> Ps2Games { get; } = new();
    public ObservableCollection<GameEntry> AppsGames { get; } = new();

    public class GameEntry
    {
        public string GameId { get; set; } = "";
        public string Name { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string GameFolder { get; set; } = "";
        public bool IsMultiDisc { get; set; }
        public int DiscNumber { get; set; } = 1;
        public override string ToString() => Name;
    }

    private bool _cheatWidescreen;
    private bool _cheatNoPal;
    private bool _cheatFixSound;
    private bool _cheatFixGraphics;

    public bool CheatWidescreen { get => _cheatWidescreen; set => SetProperty(ref _cheatWidescreen, value); }
    public bool CheatNoPal { get => _cheatNoPal; set => SetProperty(ref _cheatNoPal, value); }
    public bool CheatFixSound { get => _cheatFixSound; set => SetProperty(ref _cheatFixSound, value); }
    public bool CheatFixGraphics { get => _cheatFixGraphics; set => SetProperty(ref _cheatFixGraphics, value); }

    private string _oplRootFolder = "";
    private string _status = "";

    public ICommand SelectOplRootFolderCommand { get; }
    public ICommand ProcessAllCommand { get; }
    public ICommand GenerateElfCommand { get; }
    public ICommand GenerateCheatsCommand { get; }
    public ICommand DownloadCoversAndMetadataCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand RenameAllCommand { get; }
    public ICommand OpenStorageSettingsCommand { get; }
    public ICommand UpdateDatabaseCommand { get; }

    public string OplRootFolder { get => _oplRootFolder; set => SetProperty(ref _oplRootFolder, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public ProcessPopsViewModel(IPathsService paths, ILoggingService log, SettingsService settings)
    {
        _paths = paths;
        _log = log;
        _settings = settings;

        SelectOplRootFolderCommand = new Command(async () => await SelectOplRootFolder());
        ProcessAllCommand = new Command(async () => await ProcessAllGames());
        GenerateElfCommand = new Command(async () => await GenerateAllElfs());
        GenerateCheatsCommand = new Command(async () => await GenerateAllCheats());
        DownloadCoversAndMetadataCommand = new Command(async () => await DownloadAllCoversAndMetadata());
        RefreshCommand = new Command(RefreshGameLists);
        RenameAllCommand = new Command(async () => await RenameAllGames());
        OpenStorageSettingsCommand = new Command(OpenStorageSettings);
        UpdateDatabaseCommand = new Command(async () => await UpdateDatabase());

        RefreshFromSettings();
    }

    // ==================== REFRESCAR ====================
    public void RefreshFromSettings()
    {
        var savedRoot = _settings.DestinationFolder;
        if (!string.IsNullOrEmpty(savedRoot))
        {
            if (savedRoot != _oplRootFolder)
            {
                _oplRootFolder = savedRoot;
                OnPropertyChanged(nameof(OplRootFolder));
                if (_paths is PathsServiceAndroid androidPaths) androidPaths.RootFolder = savedRoot;
            }
            RefreshGameLists();
            Status = $"Raíz OPL: {savedRoot}";
            // Cargar la base de datos de covers desde la caché interna
            GameDatabase.Initialize(DatabaseUpdater.InternalDatabaseFolder);
        }
        else Status = "Selecciona la carpeta raíz OPL (desde Inicio o aquí).";
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
            if (_paths is PathsServiceAndroid androidPaths) androidPaths.RootFolder = path;
            RefreshGameLists();
            GameDatabase.Initialize(DatabaseUpdater.InternalDatabaseFolder);
        }
    }

    private void OpenStorageSettings()
    {
        try
        {
            var intent = new global::Android.Content.Intent(
                global::Android.Provider.Settings.ActionManageAllFilesAccessPermission);
            global::Android.App.Application.Context.StartActivity(intent);
        }
        catch { }
    }

    // ==================== ACTUALIZAR BASE DE DATOS ====================
    private async Task UpdateDatabase()
    {
        var allGameIds = Ps1Games.Select(g => g.GameId)
                        .Concat(Ps2Games.Select(g => g.GameId))
                        .Where(id => !string.IsNullOrWhiteSpace(id));

        await DatabaseUpdater.DownloadAndExtractDatabaseAsync(
            _paths.RootFolder,
            allGameIds,
            msg => MainThread.BeginInvokeOnMainThread(() => Status = msg)
        );

        // Recargar la base de datos local por si hubo cambios
        GameDatabase.Initialize(DatabaseUpdater.InternalDatabaseFolder);
    }

    // ==================== LISTAR JUEGOS ====================
    private void RefreshGameLists()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            if (!global::Android.OS.Environment.IsExternalStorageManager)
            {
                Status = "⚠️ Permiso de almacenamiento no concedido.\nPulsa 'Abrir ajustes' para activarlo.";
                Ps1Games.Clear(); Ps2Games.Clear(); AppsGames.Clear();
                return;
            }
        }

        Ps1Games.Clear(); Ps2Games.Clear(); AppsGames.Clear();
        if (_paths is PathsServiceAndroid androidPaths)
            androidPaths.EnsureOplFoldersExist();

        try
        {
            int popsCount = 0, dvdCount = 0, appsCount = 0;

            if (Directory.Exists(_paths.PopsFolder))
            {
                var vcdFiles = Directory.GetFiles(_paths.PopsFolder, "*.VCD", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.GetFiles(_paths.PopsFolder, "*.vcd", SearchOption.TopDirectoryOnly))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
                foreach (var vcd in vcdFiles) { Ps1Games.Add(BuildGameEntry(vcd, _paths.PopsFolder)); popsCount++; }
            }

            if (Directory.Exists(_paths.DvdFolder))
            {
                var isoFiles = Directory.GetFiles(_paths.DvdFolder, "*.ISO", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.GetFiles(_paths.DvdFolder, "*.iso", SearchOption.TopDirectoryOnly))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
                foreach (var iso in isoFiles) { Ps2Games.Add(BuildGameEntry(iso, _paths.DvdFolder)); dvdCount++; }
            }

            if (Directory.Exists(_paths.AppsFolder))
            {
                foreach (var elf in Directory.GetFiles(_paths.AppsFolder, "*.ELF", SearchOption.TopDirectoryOnly))
                {
                    AppsGames.Add(new GameEntry { Name = Path.GetFileNameWithoutExtension(elf), FilePath = elf, GameFolder = _paths.AppsFolder });
                    appsCount++;
                }
            }

            Status = $"Encontrados: {popsCount} VCD, {dvdCount} ISO, {appsCount} ELF.\nRaíz: {_paths.RootFolder}";
        }
        catch (Exception ex) { Status = $"Error al listar: {ex.Message}"; }
    }

    private GameEntry BuildGameEntry(string filePath, string parentFolder)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string companionFolder = Path.Combine(parentFolder, fileName);
        bool multiDisc = File.Exists(Path.Combine(parentFolder, "DISCS.TXT"));
        int discNumber = 1;
        if (multiDisc)
        {
            var upper = fileName.ToUpperInvariant();
            if (upper.Contains("CD2") || upper.Contains("DISC2")) discNumber = 2;
            else if (upper.Contains("CD3") || upper.Contains("DISC3")) discNumber = 3;
            else if (upper.Contains("CD4") || upper.Contains("DISC4")) discNumber = 4;
        }
        string cleanTitle = OplCompatibleTitle(fileName, discNumber, multiDisc);
        string gameId = ExtractGameId(filePath);
        return new GameEntry { Name = cleanTitle, FilePath = filePath, GameFolder = companionFolder, GameId = gameId, IsMultiDisc = multiDisc, DiscNumber = discNumber };
    }

    private string OplCompatibleTitle(string rawName, int discNumber, bool multiDisc)
    {
        string title = rawName;
        int dashIndex = title.IndexOf(" - ");
        if (dashIndex > 0) title = title.Substring(dashIndex + 3).Trim();
        else
        {
            var parts = title.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1 && parts[0].Length >= 4 && parts[0].Contains('_')) title = parts[1];
        }
        if (multiDisc && discNumber > 1) title += $" (CD{discNumber})";
        return title.Replace(' ', '_').Replace("'", "").Replace(":", "").Trim();
    }

    private string ExtractGameId(string path)
    {
        try { var id = GameIdDetector.DetectGameId(path); if (!string.IsNullOrWhiteSpace(id)) return id; }
        catch { }
        return GameIdDetector.DetectFromName(Path.GetFileNameWithoutExtension(path));
    }

    // ==================== PROCESAR TODO ====================
    private async Task ProcessAllGames()
    {
        if (!Ps1Games.Any() && !Ps2Games.Any()) { Status = "No hay juegos para procesar."; return; }
        await ProcessMultidisc();
        await GenerateAllElfs();
        await GenerateAllCheats();
        await DownloadAllCoversAndMetadata();
        Status = "Procesamiento completo.";
    }

    // ==================== GENERAR ELFS ====================
    private async Task GenerateAllElfs()
    {
        Status = "Generando ELFs...";
        string baseElf = _paths.PopstarterElfPath;
        if (!File.Exists(baseElf))
        {
            Status = $"POPSTARTER.ELF no encontrado.\nSe buscó en:\n" +
                     $"{Path.Combine(_paths.RootFolder, "POPSTARTER.ELF")}\n" +
                     $"{Path.Combine(_paths.PopsFolder, "POPSTARTER.ELF")}\n" +
                     "Cópialo a una de esas ubicaciones.";
            await Task.CompletedTask;
            return;
        }

        await ProcessMultidisc();

        int generated = 0, skipped = 0;

        foreach (var game in Ps1Games)
        {
            if (!File.Exists(game.FilePath))
            {
                _log.Log($"[ELF] No se encontró VCD: {game.FilePath}");
                continue;
            }

            string elfFileName = $"{Path.GetFileNameWithoutExtension(game.FilePath)}.ELF";
            string elfPath;

            if (game.IsMultiDisc)
            {
                elfPath = Path.Combine(game.GameFolder, elfFileName);
            }
            else
            {
                string gameAppFolder = Path.Combine(_paths.AppsFolder, game.Name);
                Directory.CreateDirectory(gameAppFolder);
                elfPath = Path.Combine(gameAppFolder, elfFileName);
            }

            if (!File.Exists(elfPath))
            {
                ElfGenerator.GeneratePs1Elf(baseElf, game.FilePath, elfPath, game.DiscNumber, game.Name, game.GameId, msg => _log.Log(msg));
                generated++;
            }
            else
            {
                _log.Log($"[ELF] Ya existe: {elfFileName}");
                skipped++;
            }

            if (!game.IsMultiDisc)
            {
                string titleCfgPath = Path.Combine(Path.GetDirectoryName(elfPath)!, "title.cfg");
                if (!File.Exists(titleCfgPath))
                {
                    try
                    {
                        string cfgContent = $"title={game.Name}\nboot={elfFileName}\n";
                        File.WriteAllText(titleCfgPath, cfgContent);
                    }
                    catch (Exception ex) { _log.Log($"[ELF] Error creando title.cfg: {ex.Message}"); }
                }
            }
        }

        Status = generated > 0
            ? $"{generated} ELFs generados. {skipped} ya existían."
            : $"No se generaron ELFs. {skipped} ya existían.";
        await Task.CompletedTask;
    }

    // ==================== MULTIDISCO ====================
    private async Task ProcessMultidisc()
    {
        var ps1Groups = Ps1Games
            .Where(g => !string.IsNullOrEmpty(g.GameId))
            .GroupBy(g => new string(g.GameId.TakeWhile(c => c != '.' && c != '_').ToArray()))
            .ToList();

        foreach (var group in ps1Groups)
        {
            var discs = group.OrderBy(g => g.DiscNumber).ToList();
            if (discs.Count <= 1) continue;

            string baseFolderName = discs.First().Name.Replace(" (CD1)", "").Replace(" (CD2)", "").Replace(" (CD3)", "").Replace(" (CD4)", "").Trim();
            string commonFolder = Path.Combine(Path.GetDirectoryName(discs.First().FilePath)!, baseFolderName);
            Directory.CreateDirectory(commonFolder);

            string discsTxtPath = Path.Combine(commonFolder, "DISCS.TXT");
            var discNames = discs.Select(d => Path.GetFileName(d.FilePath)).ToList();
            if (!File.Exists(discsTxtPath))
            {
                await File.WriteAllLinesAsync(discsTxtPath, discNames);
                _log.Log($"[Multidisco] Creado {discsTxtPath}");
            }

            string baseElf = _paths.PopstarterElfPath;
            if (File.Exists(baseElf))
            {
                foreach (var disc in discs)
                {
                    disc.GameFolder = commonFolder;
                    string elfFileName = $"{Path.GetFileNameWithoutExtension(disc.FilePath)}.ELF";
                    string elfPath = Path.Combine(commonFolder, elfFileName);
                    if (!File.Exists(elfPath))
                        ElfGenerator.GeneratePs1Elf(baseElf, disc.FilePath, elfPath, disc.DiscNumber, disc.Name, disc.GameId, msg => _log.Log(msg));
                }
            }

            string titleCfgPath = Path.Combine(commonFolder, "title.cfg");
            if (!File.Exists(titleCfgPath))
            {
                string elfFileName = $"{Path.GetFileNameWithoutExtension(discs.First().FilePath)}.ELF";
                await File.WriteAllTextAsync(titleCfgPath, $"title={baseFolderName}\nboot={elfFileName}\n");
            }
        }

        await Task.CompletedTask;
    }

    // ==================== GENERAR CHEATS ====================
    private async Task GenerateAllCheats()
    {
        Status = "Generando cheats...";
        var extraLines = new List<string>();
        if (_cheatWidescreen) extraLines.Add("WIDESCREEN=ON");
        if (_cheatNoPal) extraLines.Add("$NOPAL");
        if (_cheatFixSound) extraLines.Add("FIXSOUND=ON");
        if (_cheatFixGraphics) extraLines.Add("FIXGRAPHICS=ON");

        int generated = 0, skipped = 0;

        foreach (var game in Ps1Games)
        {
            Directory.CreateDirectory(game.GameFolder);
            string cheatFile = Path.Combine(game.GameFolder, "CHEAT.TXT");

            if (!File.Exists(cheatFile))
            {
                CheatGenerator.GenerateCheatTxt(game.GameId, game.GameFolder, extraLines, msg => _log.Log(msg));
                generated++;
            }
            else
            {
                _log.Log($"[Cheats] Ya existe: {cheatFile}");
                skipped++;
            }
        }

        Status = generated > 0
            ? $"{generated} CHEAT.TXT generados. {skipped} ya existían."
            : $"No se generaron cheats. {skipped} ya existían.";
        await Task.CompletedTask;
    }

    // ==================== DESCARGAR COVERS Y METADATOS ====================
    private async Task DownloadAllCoversAndMetadata()
    {
        var allGames = Ps1Games.Concat(Ps2Games).ToList();
        if (!allGames.Any()) { Status = "No hay juegos para actualizar."; return; }

        int coversDownloaded = 0, coversSkipped = 0, metaCopied = 0, metaSkipped = 0;
        string mirrorBase = "https://archive.org/download/oplm-art-2023-11";
        string sourceCfgFolder = Path.Combine(DatabaseUpdater.InternalDatabaseFolder, "CFG");
        bool internalDbExists = Directory.Exists(sourceCfgFolder);

        for (int i = 0; i < allGames.Count; i++)
        {
            var game = allGames[i];
            MainThread.BeginInvokeOnMainThread(() => Status = $"{i + 1}/{allGames.Count}: {game.Name}");

            // ----- COVER -----
            string artFile = Path.Combine(_paths.ArtFolder, game.GameId + ".jpg");
            if (!File.Exists(artFile))
            {
                string? url = GameDatabase.TryGetCoverUrl(game.GameId) ?? $"{mirrorBase}/ART/{game.GameId}.jpg";
                if (await DownloadFileAsync(url, artFile))
                {
                    try { ArtResizer.ResizeToArt(artFile, artFile.Replace(".jpg", ".ART"), msg => _log.Log(msg)); }
                    catch { }
                    coversDownloaded++;
                }
                else
                {
                    _log.Log($"[Cover] No disponible para {game.Name}");
                }
            }
            else { coversSkipped++; }

            // ----- METADATOS -----
            string destCfgFile = Path.Combine(_paths.CfgFolder, game.GameId + ".cfg");
            if (!File.Exists(destCfgFile))
            {
                if (internalDbExists)
                {
                    string srcCfgFile = Path.Combine(sourceCfgFolder, game.GameId + ".cfg");
                    if (File.Exists(srcCfgFile))
                    {
                        try { File.Copy(srcCfgFile, destCfgFile); metaCopied++; }
                        catch { _log.Log($"[Meta] Error copiando {game.GameId}.cfg"); }
                    }
                    else
                    {
                        _log.Log($"[Meta] No encontrado en base de datos: {game.GameId}");
                    }
                }
                else
                {
                    _log.Log("[Meta] Base de datos interna no encontrada. Usa 'Actualizar BD' primero.");
                }
            }
            else { metaSkipped++; }
        }

        string msg = $"Covers: {coversDownloaded} descargados, {coversSkipped} ya existían.\n" +
                     $"Metadatos: {metaCopied} copiados, {metaSkipped} ya existían.";
        if (!internalDbExists) msg += "\n⚠️ Actualiza la base de datos primero.";
        Status = msg;
    }

    // ==================== RENOMBRAR JUEGOS ====================
    private async Task RenameAllGames()
    {
        if (!Ps1Games.Any() && !Ps2Games.Any()) { Status = "No hay juegos para renombrar."; return; }

        int renamed = 0, skipped = 0;
        var errors = new List<string>();

        foreach (var game in Ps1Games.ToList())
        {
            try
            {
                string folder = Path.GetDirectoryName(game.FilePath)!;
                string newName = $"{game.GameId}.{game.Name}.VCD";
                string newPath = Path.Combine(folder, newName);
                if (string.Equals(game.FilePath, newPath, StringComparison.OrdinalIgnoreCase)) { skipped++; continue; }

                File.Move(game.FilePath, newPath);
                game.FilePath = newPath;
                if (Directory.Exists(game.GameFolder))
                {
                    string newFolderPath = Path.Combine(folder, $"{game.GameId}.{game.Name}");
                    Directory.Move(game.GameFolder, newFolderPath);
                    game.GameFolder = newFolderPath;
                }
                renamed++;
            }
            catch (Exception ex) { errors.Add($"{game.Name}: {ex.Message}"); }
        }

        foreach (var game in Ps2Games.ToList())
        {
            try
            {
                string folder = Path.GetDirectoryName(game.FilePath)!;
                string newName = $"{game.GameId}.{game.Name}.iso";
                string newPath = Path.Combine(folder, newName);
                if (string.Equals(game.FilePath, newPath, StringComparison.OrdinalIgnoreCase)) { skipped++; continue; }

                File.Move(game.FilePath, newPath);
                game.FilePath = newPath;
                if (Directory.Exists(game.GameFolder))
                {
                    string newFolderPath = Path.Combine(folder, $"{game.GameId}.{game.Name}");
                    Directory.Move(game.GameFolder, newFolderPath);
                    game.GameFolder = newFolderPath;
                }
                renamed++;
            }
            catch (Exception ex) { errors.Add($"{game.Name}: {ex.Message}"); }
        }

        Ps1Games.Clear(); Ps2Games.Clear();
        RefreshGameLists();

        string result = $"Renombrados: {renamed}. Omitidos: {skipped} (ya tenían el formato).";
        if (errors.Any()) result += $" Errores: {string.Join("; ", errors)}";
        Status = result;
        await Task.CompletedTask;
    }

    // ==================== MÉTODOS AUXILIARES ====================
    private async Task<bool> DownloadFileAsync(string url, string destination)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await using var fs = new FileStream(destination, FileMode.Create);
            await response.Content.CopyToAsync(fs);
            return true;
        }
        catch { return false; }
    }

    protected bool SetProperty<T>(ref T backingStore, T value,
        [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}