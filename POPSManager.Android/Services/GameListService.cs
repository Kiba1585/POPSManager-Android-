using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using POPSManager.Core.Logic;
using POPSManager.Core.Services;
using POPSManager.Android.Models;

namespace POPSManager.Android.Services
{
    public class GameListService
    {
        private readonly IPathsService _paths;
        private readonly ILoggingService _log;

        public ObservableCollection<GameItem> Ps1Games { get; } = new();
        public ObservableCollection<GameItem> Ps2Games { get; } = new();
        public ObservableCollection<GameItem> AppsGames { get; } = new();

        public GameListService(IPathsService paths, ILoggingService log)
        {
            _paths = paths;
            _log = log;
        }

        public string Refresh()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                if (!global::Android.OS.Environment.IsExternalStorageManager)
                {
                    Ps1Games.Clear(); Ps2Games.Clear(); AppsGames.Clear();
                    return "⚠️ Permiso de almacenamiento no concedido.\nPulsa 'Abrir ajustes' para activarlo.";
                }
            }

            Ps1Games.Clear(); Ps2Games.Clear(); AppsGames.Clear();
            if (_paths is PathsServiceAndroid androidPaths)
                androidPaths.EnsureOplFoldersExist();

            try
            {
                int pops = 0, dvd = 0, apps = 0;

                if (Directory.Exists(_paths.PopsFolder))
                {
                    var vcds = Directory.GetFiles(_paths.PopsFolder, "*.VCD", SearchOption.TopDirectoryOnly)
                        .Concat(Directory.GetFiles(_paths.PopsFolder, "*.vcd", SearchOption.TopDirectoryOnly))
                        .Distinct(StringComparer.OrdinalIgnoreCase);
                    foreach (var v in vcds) { Ps1Games.Add(BuildItem(v, _paths.PopsFolder)); pops++; }
                }

                if (Directory.Exists(_paths.DvdFolder))
                {
                    var isos = Directory.GetFiles(_paths.DvdFolder, "*.ISO", SearchOption.TopDirectoryOnly)
                        .Concat(Directory.GetFiles(_paths.DvdFolder, "*.iso", SearchOption.TopDirectoryOnly))
                        .Distinct(StringComparer.OrdinalIgnoreCase);
                    foreach (var i in isos) { Ps2Games.Add(BuildItem(i, _paths.DvdFolder)); dvd++; }
                }

                if (Directory.Exists(_paths.AppsFolder))
                {
                    foreach (var e in Directory.GetFiles(_paths.AppsFolder, "*.ELF", SearchOption.TopDirectoryOnly))
                    {
                        AppsGames.Add(new GameItem { Name = Path.GetFileNameWithoutExtension(e), FilePath = e, GameFolder = _paths.AppsFolder });
                        apps++;
                    }
                }

                var sample = Ps1Games.Take(3).Select(g => g.Name);
                return $"Encontrados: {pops} VCD, {dvd} ISO, {apps} ELF.\nEjemplos: {string.Join(", ", sample)}";
            }
            catch (Exception ex) { return $"Error al listar: {ex.Message}"; }
        }

        private GameItem BuildItem(string filePath, string parentFolder)
        {
            string fname = Path.GetFileNameWithoutExtension(filePath);
            string comp = Path.Combine(parentFolder, fname);
            bool multi = File.Exists(Path.Combine(parentFolder, "DISCS.TXT"));
            int disc = 1;
            if (multi)
            {
                var u = fname.ToUpperInvariant();
                if (u.Contains("CD2") || u.Contains("DISC2")) disc = 2;
                else if (u.Contains("CD3") || u.Contains("DISC3")) disc = 3;
                else if (u.Contains("CD4") || u.Contains("DISC4")) disc = 4;
            }

            string orig = ExtractGameId(filePath);
            string norm = NormalizeGameId(orig);
            string title = OplCompatibleTitle(fname, disc, multi);

            return new GameItem
            {
                Name = title,
                FilePath = filePath,
                GameFolder = comp,
                OriginalGameId = orig,
                GameId = norm,
                IsMultiDisc = multi,
                DiscNumber = disc
            };
        }

        // ========== Utilidades de nombre ==========
        public static string OplCompatibleTitle(string raw, int discNum, bool multi)
        {
            string t = raw;
            int dash = t.IndexOf(" - ");
            if (dash > 0) t = t.Substring(dash + 3).Trim();
            else if (t.Contains(' '))
            {
                var parts = t.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1 && parts[0].Length >= 4 && parts[0].Contains('_')) t = parts[1];
            }
            else if (t.Contains('.'))
            {
                int dot = t.IndexOf('.');
                if (dot > 0 && t.Substring(0, dot).Length >= 4 && t.Substring(0, dot).Contains('_'))
                    t = t.Substring(dot + 1).Trim();
            }

            t = Regex.Replace(t, @"\s*\((?!CD\d)[^)]*\)\s*", " ", RegexOptions.IgnoreCase);
            t = t.Replace("'", "").Replace(":", "").Replace("[", "").Replace("]", "")
                 .Replace(",,", ",").Replace(",,", ",").Replace(",", "");
            t = Regex.Replace(t, @"[\s_]+", "_").Trim('_');

            if (multi && discNum > 1) t += $" (CD{discNum})";
            return AbbreviateIfTooLong(t, 32);
        }

        public static string AbbreviateIfTooLong(string title, int max = 32)
        {
            if (title.Length <= max) return title;
            int dash = title.IndexOf(" - ");
            string baseP = dash > 0 ? title.Substring(0, dash).Trim() : title;
            string subP = dash > 0 ? title.Substring(dash + 3).Trim() : "";
            var words = baseP.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= 1) return title;
            string abbr = string.Concat(words.Select(w => char.ToUpper(w[0])));
            string res = string.IsNullOrEmpty(subP) ? abbr : $"{abbr} - {subP}";
            return res.Length <= max ? res : res.Substring(0, max).Trim();
        }

        private static string NormalizeGameId(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            return new string(raw.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        }

        private static string ExtractGameId(string path)
        {
            try { var id = GameIdDetector.DetectGameId(path); if (!string.IsNullOrWhiteSpace(id)) return id; }
            catch { }
            return GameIdDetector.DetectFromName(Path.GetFileNameWithoutExtension(path));
        }
    }
}