using Android.Content;
using AndroidX.Core.Content;
using CommunityToolkit.Maui.Storage;
using POPSManager.Core.Services;
using System.IO;
using System.Threading.Tasks;
using AndroidUri = Android.Net.Uri;

namespace POPSManager.Android.Services;

public class PathsServiceAndroid : IPathsService
{
    public string RootFolder { get; set; } = "/storage/emulated/0/POPSManager";
    public string PopsFolder => Path.Combine(RootFolder, "POPS");
    public string AppsFolder => Path.Combine(RootFolder, "APPS");
    public string CfgFolder => Path.Combine(RootFolder, "CFG");
    public string ArtFolder => Path.Combine(RootFolder, "ART");
    public string DvdFolder => Path.Combine(RootFolder, "DVD");
    public string PopstarterElfPath { get; set; } = "";
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
            var context = Android.App.Application.Context;
            var uri = FileProvider.GetUriForFile(context,
                context.PackageName + ".fileprovider",
                new Java.IO.File(folderPath));

            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(uri, "resource/folder"); // fallback general
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            intent.AddFlags(ActivityFlags.NewTask);

            // Intentar abrir con un explorador de archivos conocido
            if (intent.ResolveActivity(context.PackageManager) == null)
            {
                // Alternativa con esquema content://
                var docUri = AndroidUri.Parse("content://com.android.externalstorage.documents/tree/" +
                    folderPath.Replace("/storage/emulated/0/", "primary%3A"));
                intent.SetDataAndType(docUri, DocumentsContract.Document.MimeTypeDir);
            }

            context.StartActivity(intent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("No se pudo abrir la carpeta: " + ex.Message);
        }
    }

    public void SetElfPath(string path) => PopstarterElfPath = path;
    public void SetPs2ElfPath(string path) => PopstarterPs2ElfPath = path;

    // Interfaz IPathsService requiere estos métodos extra? Asegurémonos de que existen.
    public async Task<string?> SelectFolderAsync() { ... } // ya implementado
}