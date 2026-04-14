namespace POPSManager.Core.Services;

public static class MultiDiscManager
{
    public static bool IsDisc(string name)
    {
        return name.Contains("Disc 2", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("CD2", StringComparison.OrdinalIgnoreCase);
    }
}
