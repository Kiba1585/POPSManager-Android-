using POPSManager.Models;
using POPSManager.Services;
using POPSManager.UI.Localization;
using System;
using System.IO;
using System.Linq;

namespace POPSManager.Logic
{
    /// <summary>
    /// Conversor legacy de BIN/CUE/ISO a VCD.
    /// Se recomienda usar ConverterService para nuevas funcionalidades.
    /// </summary>
    public class Converter
    {
        private readonly Action<int> _updateProgress;
        private readonly Action<string> _updateSpinner;
        private readonly Action<string> _log;
        private readonly Action<UiNotification> _notify;
        private readonly PathsService _paths;
        private readonly LocalizationService _loc;

        public Converter(
            Action<int> updateProgress,
            Action<string> updateSpinner,
            Action<string> log,
            Action<UiNotification> notify,
            PathsService paths,
            LocalizationService loc)
        {
            _updateProgress = updateProgress;
            _updateSpinner = updateSpinner;
            _log = log;
            _notify = notify;
            _paths = paths;
            _loc = loc;
        }

        public void ConvertSingle(string inputPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputPath);

            // ---------------------------------------------------------
            //  CUE → BIN
            // ---------------------------------------------------------
            if (inputPath.EndsWith(".cue", StringComparison.OrdinalIgnoreCase))
            {
                var cue = CueParser.Parse(inputPath, _log);
                if (cue == null)
                {
                    _notify(new UiNotification
                    {
                        Type = NotificationType.Error,
                        Message = string.Format(_loc.GetString("Converter_InvalidCue"), fileName)
                    });
                    return;
                }

                inputPath = cue.BinPath;
                fileName = Path.GetFileNameWithoutExtension(cue.BinPath);
            }

            // ---------------------------------------------------------
            //  IGNORAR PS2
            // ---------------------------------------------------------
            if (IsPs2Iso(inputPath))
            {
                _log(string.Format(_loc.GetString("Converter_Ps2IsoIgnored"), inputPath));
                return;
            }

            string outputPath = Path.Combine(_paths.PopsFolder, fileName + ".VCD");

            _log("-----------------------------------------");
            _log(string.Format(_loc.GetString("Converter_Converting"), fileName));

            using var input = File.OpenRead(inputPath);
            using var output = File.Create(outputPath);

            var mode = SectorDetector.Detect(input, _log);
            if (mode == SectorMode.Unknown)
            {
                _notify(new UiNotification
                {
                    Type = NotificationType.Error,
                    Message = string.Format(_loc.GetString("Converter_UnknownSectorFormat"), fileName)
                });
                return;
            }

            // ---------------------------------------------------------
            //  HEADER + CONVERSIÓN
            // ---------------------------------------------------------
            VcdHeader.Write(output, fileName, input.Length, _log);
            SectorConverter.Convert(input, output, mode, _updateProgress, _log);

            _notify(new UiNotification
            {
                Type = NotificationType.Success,
                Message = string.Format(_loc.GetString("Converter_VcdGenerated"), fileName)
            });
        }

        private static bool IsPs2Iso(string path)
        {
            string name = Path.GetFileName(path).ToUpperInvariant();

            string[] ps2Patterns =
            {
                "SLUS_2", "SLUS_3",
                "SCUS_9",
                "SLES_5", "SLES_6",
                "SCES_5", "SCES_6"
            };

            return ps2Patterns.Any(p => name.Contains(p));
        }
    }
}