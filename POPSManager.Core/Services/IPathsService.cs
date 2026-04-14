namespace POPSManager.Core.Services;

public interface IPathsService
{
    Task<string?> SelectFolderAsync();
}
