using Android.Content;
using AndroidX.Core.Content;
using CommunityToolkit.Maui.Storage;
using POPSManager.Core.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using AndroidUri = Android.Net.Uri;

namespace POPSManager.Android.Services;

public class PathsServiceAndroid : IPathsService
{
    private string _rootFolder = "/storage/emulated/0/POPSManager";

    public string RootFolder
    {
        get => _rootFolder;
        set
        {
            if (_rootFolder != value)
            {
                _rootFolder = value;
                // Al cambiar la raíz, aseguramos que las carpetas existan
                EnsureOplFoldersExist();
            }
        }
    }

    public string PopsFolder => Path.Combine(RootFolder, "POPS");
    public string AppsFolder => Path.Combine(RootFolder, "APPS");
    public string CfgFolder => Path.Combine(RootFolder, "CFG");
    public string ArtFolder => Path.Combine(RootFolder, "ART");
    public string DvdFolder => Path.Combine(RootFolder, "DVD");

    // POPSTARTER.ELF base se espera en la raíz OPL
    public string PopstarterElfPath => Path.Combine(RootFolder, "POPSTARTER.ELF");
    public string PopstarterPs2ElfPath { get; set; } = "";

    public string TempFolder => FileSystem.CacheDirectory;
    public string SafeOutputFolder => Path.Combine(FileSystem.AppDataDirectory, "Output");

    public async Task<string?> SelectFolderAsync()
    {
        var result = await FolderPicker.Default.PickAsync(default);
        return result?.Folder?.Path;
    }

    public async Task<string?> SelectFileAsync(string filter = "*/*")
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Selecciona un archivo",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { filter } }
            })
        });
        return result?.FullPath;
    }

    public void EnsureOplFoldersExist()
    {
        try
        {
            Directory.CreateDirectory(RootFolder);
            Directory.CreateDirectory(PopsFolder);
            Directory.CreateDirectory(DvdFolder);
            Directory.CreateDirectory(CfgFolder);
            Directory.CreateDirectory(ArtFolder);
            Directory.CreateDirectory(AppsFolder);
            System.Diagnostics.Debug.WriteLine($"Carpetas OPL creadas bajo {RootFolder}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al crear carpetas OPL: {ex.Message}");
        }
    }

    public bool IsFolderWritable(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return false;

        try
        {
            var testFile = Path.Combine(folderPath, ".writetest");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void OpenFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return;

        try
        {
            var context = global::Android.App.Application.Context;
            var packageManager = context.PackageManager;
            if (packageManager is null) return;

            var androidUri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                context,
                context.PackageName + ".fileprovider",
                new Java.IO.File(folderPath));

            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(androidUri, "resource/folder");
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            intent.AddFlags(ActivityFlags.NewTask);

            if (intent.ResolveActivity(packageManager) == null)
            {
                var docUri = AndroidUri.Parse(
                    "content://com.android.externalstorage.documents/tree/" +
                    folderPath.Replace("/storage/emulated/0/", "primary%3A"));
                intent.SetDataAndType(docUri, "resource/folder");
            }

            context.StartActivity(intent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("No se pudo abrir la carpeta: " + ex.Message);
        }
    }

    // Ya no se necesitan SetElfPath ni SetPs2ElfPath, pero se mantienen por la interfaz
    public void SetElfPath(string path) { /* ignorado, automático */ }
    public void SetPs2ElfPath(string path) => PopstarterPs2ElfPath = path;
}