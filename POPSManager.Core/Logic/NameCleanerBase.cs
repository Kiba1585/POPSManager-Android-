using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace POPSManager.Core.Logic
{
    public static class NameCleanerBase
    {
        private static readonly string[] MinorWords = { "of", "the", "and", "to", "in", "on", "at", "for", "from", "a", "an" };
        private static readonly Regex DiscRegex = new(@"(?:DISC|DISK|CD)[\s\-_]*0?(\d{1,2})|(?:D)(\d{1,2})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TrashParenthesis = new(@"\((PAL|NTSC|NTSC-U|NTSC-J|ESP|ES|EN|ENG|FRA|GER|ITA|MULTI\d?|v\d+\.\d+|Rev\s*\d+|Beta|Demo|Track\s*\d+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegionRegex = new(@"\b(PAL|NTSC|NTSC-U|NTSC-J)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex LanguageRegex = new(@"\b(ESP|ES|EN|ENG|FRA|GER|ITA|MULTI|MULTI\d?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex VersionRegex = new(@"\b(v\d+\.\d+|Rev\s*\d+|Beta|Demo)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex EmbeddedIdRegex = new(@"\b(SCES|SLES|SCUS|SLUS|SLPS|SLPM|SCPS)[-_]?\d+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BadSymbolsRegex = new(@"[\[\]\{\}#@%&]+", RegexOptions.Compiled);
        private static readonly Regex SpaceNormalizer = new(@"\s{2,}", RegexOptions.Compiled);
        private static readonly Regex UnderscoreDotNormalizer = new(@"[_\.]+", RegexOptions.Compiled);

        public static string Clean(string name, out string? cdTag)
        {
            cdTag = DetectDisc(name);
            name = TrashParenthesis.Replace(name, "");
            name = RegionRegex.Replace(name, "");
            name = LanguageRegex.Replace(name, "");
            name = VersionRegex.Replace(name, "");
            name = EmbeddedIdRegex.Replace(name, "");
            name = BadSymbolsRegex.Replace(name, "");
            name = UnderscoreDotNormalizer.Replace(name, " ");
            name = SpaceNormalizer.Replace(name, " ");
            return ToTitleCaseSmart(name.Trim());
        }

        public static string CleanTitleOnly(string name)
        {
            name = TrashParenthesis.Replace(name, "");
            name = RegionRegex.Replace(name, "");
            name = LanguageRegex.Replace(name, "");
            name = VersionRegex.Replace(name, "");
            name = EmbeddedIdRegex.Replace(name, "");
            name = BadSymbolsRegex.Replace(name, "");
            name = UnderscoreDotNormalizer.Replace(name, " ");
            name = SpaceNormalizer.Replace(name, " ");
            return ToTitleCaseSmart(name.Trim());
        }

        private static string? DetectDisc(string name)
        {
            var m = DiscRegex.Match(name);
            if (!m.Success) return null;
            if (m.Groups[1].Success) return $"CD{m.Groups[1].Value}";
            return m.Groups[2].Success ? $"CD{m.Groups[2].Value}" : null;
        }

        private static string ToTitleCaseSmart(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i].ToLowerInvariant();
                words[i] = (i > 0 && MinorWords.Contains(w)) ? w : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(w);
            }
            return string.Join(" ", words);
        }
    }
}