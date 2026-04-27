using System.Text.RegularExpressions;

namespace POPSManager.Core.Logic
{
    public static class GameIdValidator
    {
        private static readonly Regex Ps1Regex =
            new(@"^(SCES|SLES|SCUS|SLUS|SLPS|SLPM|SCPS)_[0-9]{5}$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex Ps2Regex =
            new(@"^(SLUS|SCUS|SLES|SCES|SLPM|SLPS)_[0-9]{5}$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsPs1(string id) => !string.IsNullOrWhiteSpace(id) && Ps1Regex.IsMatch(id);
        public static bool IsPs2(string id) => !string.IsNullOrWhiteSpace(id) && Ps2Regex.IsMatch(id);
    }
}