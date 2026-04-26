using System.Globalization;

namespace POPSManager.Core.Services;

public static class NameCleaner
{
    public static string Clean(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;

        name = name.Replace("_", " ")
                   .Replace("-", " ")
                   .Trim();

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
    }
}