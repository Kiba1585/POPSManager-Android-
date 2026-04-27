using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using POPSManager.Core.Logic.Automation;
using POPSManager.Core.Logic.MultiDisc;

namespace POPSManager.Core.Logic
{
    public static class MultiDiscManager
    {
        private static readonly Regex SimpleDiscRegex = new(@"(?:disc|disk|cd)[\s\-_]*0?(\d{1,2})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static int ExtractDiscNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 1;
            var m = SimpleDiscRegex.Match(input);
            return m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0 ? n : 1;
        }

        public static async Task GenerateDiscsTxtAsync(string popsFolder, string gameId, List<string> discPaths, Action<string> log, AutomationEngine? auto = null)
        {
            if (discPaths == null || discPaths.Count == 0) return;
            var discs = discPaths.Select(p => new DiscInfo
            {
                Path = p,
                DiscNumber = DiscDetector.DetectDiscNumber(p, log),
                GameId = GameIdDetector.DetectGameId(p) ?? "",
                FolderName = Path.GetFileName(Path.GetDirectoryName(p)) ?? "",
                FileName = Path.GetFileName(p) ?? ""
            }).ToList();
            if (!DiscValidator.Validate(discs, log)) return;
            var lines = discs.Select(d => $"mass:/POPS/{d.FolderName}/{d.FileName}").ToList();
            foreach (var d in discs) await File.WriteAllLinesAsync(Path.Combine(Path.GetDirectoryName(d.Path)!, "DISCS.TXT"), lines);
        }

        public static void GenerateDiscsTxt(string popsFolder, string gameId, List<string> discPaths, Action<string> log, AutomationEngine? auto = null) =>
            GenerateDiscsTxtAsync(popsFolder, gameId, discPaths, log, auto).GetAwaiter().GetResult();
    }
}