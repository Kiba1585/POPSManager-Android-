using POPSManager.Core.Services;

namespace POPSManager.Android.Services;

public class PathsServiceAndroid : IPathsService
{
    public async Task<string?> SelectFolderAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Seleccione una carpeta"
        });

        return result?.FullPath;
    }
}