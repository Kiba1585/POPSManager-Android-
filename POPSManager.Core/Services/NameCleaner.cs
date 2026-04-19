namespace POPSManager.Core.Services;

public static class NameCleaner
{
    public static string Clean(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        name = name.Replace("_", " ")
                   .Replace("-", " ")
                   .Trim();

        return System.Globalization.CultureInfo.CurrentCulture
            .TextInfo
            .ToTitleCase(name.ToLower());
    }
}