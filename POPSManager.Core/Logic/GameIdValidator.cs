using System.Text.RegularExpressions;

namespace POPSManager.Logic
{
    public static class GameIdValidator
    {
        // PS1 → 4 letras + "_" + 5 números = 10 chars
        private static readonly Regex Ps1Regex =
            new(@"^(SCES|SLES|SCUS|SLUS|SLPS|SLPM|SCPS)_[0-9]{5}$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // PS2 → 4 letras + "_" + 5 números = 11 chars
        private static readonly Regex Ps2Regex =
            new(@"^(SLUS|SCUS|SLES|SCES|SLPM|SLPS)_[0-9]{5}$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsPs1(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            return Ps1Regex.IsMatch(id);
        }

        public static bool IsPs2(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            return Ps2Regex.IsMatch(id);
        }
    }
}