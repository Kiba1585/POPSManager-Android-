using POPSManager.Settings;
using POPSManager.Services;
using System;
using System.Threading.Tasks;

namespace POPSManager.Logic.Automation
{
    public sealed class AutomationEngine
    {
        private readonly SettingsService _settings;
        private readonly NotificationService _notify;
        private readonly LoggingService _log;

        public Func<string, Task<bool>>? OnAskUserAsync { get; set; }

        public AutomationEngine(SettingsService settings, NotificationService notify, LoggingService log)
        {
            _settings = settings;
            _notify = notify;
            _log = log;
        }

        public AutomationMode Mode => _settings.Automation.Mode;

        private bool IsAutomatic => Mode == AutomationMode.Automatico;
        private bool IsAssisted => Mode == AutomationMode.Asistido;
        private bool IsManual => Mode == AutomationMode.Manual;

        private async Task<bool> DecideAsync(string actionName, AutoBehavior behavior)
        {
            if (behavior == AutoBehavior.Automatico || IsAutomatic)
            {
                _log.Info($"[AUTO] {actionName}: automático → permitido");
                return true;
            }

            if (behavior == AutoBehavior.Manual || IsManual)
            {
                _log.Info($"[AUTO] {actionName}: manual → bloqueado");
                return false;
            }

            if (IsAssisted || behavior == AutoBehavior.Preguntar)
            {
                if (OnAskUserAsync == null)
                {
                    _log.Warn($"[AUTO] {actionName}: no hay UI para preguntar → denegado");
                    return false;
                }

                _notify.Info($"¿Deseas permitir la acción: {actionName}?");
                bool result = await OnAskUserAsync(actionName);

                _log.Info($"[AUTO] {actionName}: usuario respondió → {result}");
                return result;
            }

            _log.Warn($"[AUTO] {actionName}: comportamiento desconocido → denegado");
            return false;
        }

        // Conversión
        public bool ShouldConvert() =>
            DecideAsync("Conversión", _settings.Automation.Conversion).GetAwaiter().GetResult();
        public Task<bool> ShouldConvertAsync() =>
            DecideAsync("Conversión", _settings.Automation.Conversion);

        // Base de datos
        public bool ShouldUseDatabase() =>
            DecideAsync("Base de datos", _settings.Automation.Database).GetAwaiter().GetResult();
        public Task<bool> ShouldUseDatabaseAsync() =>
            DecideAsync("Base de datos", _settings.Automation.Database);

        // Carátulas
        public bool ShouldDownloadCovers() =>
            DecideAsync("Descarga de carátulas", _settings.Automation.Covers).GetAwaiter().GetResult();
        public Task<bool> ShouldDownloadCoversAsync() =>
            DecideAsync("Descarga de carátulas", _settings.Automation.Covers);

        // Multidisco
        public bool ShouldHandleMultiDisc() =>
            DecideAsync("Multidisco", _settings.Automation.MultiDisc).GetAwaiter().GetResult();
        public Task<bool> ShouldHandleMultiDiscAsync() =>
            DecideAsync("Multidisco", _settings.Automation.MultiDisc);

        // Cheats
        public bool ShouldGenerateCheats() =>
            DecideAsync("Generación de CHEAT.TXT", _settings.Automation.Cheats).GetAwaiter().GetResult();
        public Task<bool> ShouldGenerateCheatsAsync() =>
            DecideAsync("Generación de CHEAT.TXT", _settings.Automation.Cheats);

        // Creación de carpetas
        public bool ShouldCreateFolders() =>
            DecideAsync("Creación de carpetas", _settings.Automation.FolderCreation).GetAwaiter().GetResult();
        public Task<bool> ShouldCreateFoldersAsync() =>
            DecideAsync("Creación de carpetas", _settings.Automation.FolderCreation);

        // Notificaciones
        public bool ShouldShowNotifications() =>
            DecideAsync("Notificaciones", _settings.Automation.Notifications).GetAwaiter().GetResult();
        public Task<bool> ShouldShowNotificationsAsync() =>
            DecideAsync("Notificaciones", _settings.Automation.Notifications);

        // ELF
        public bool ShouldGenerateElf() =>
            DecideAsync("Generación de ELF", _settings.Automation.ElfGeneration).GetAwaiter().GetResult();
        public Task<bool> ShouldGenerateElfAsync() =>
            DecideAsync("Generación de ELF", _settings.Automation.ElfGeneration);

        // Metadatos
        public bool ShouldUseMetadata() =>
            DecideAsync("Generación de metadatos (.cfg)", _settings.Automation.Metadata).GetAwaiter().GetResult();
        public Task<bool> ShouldUseMetadataAsync() =>
            DecideAsync("Generación de metadatos (.cfg)", _settings.Automation.Metadata);

        // LNG
        public bool ShouldCopyLng() =>
            DecideAsync("Copia de archivos LNG", _settings.Automation.Lng).GetAwaiter().GetResult();
        public Task<bool> ShouldCopyLngAsync() =>
            DecideAsync("Copia de archivos LNG", _settings.Automation.Lng);

        // THM
        public bool ShouldCopyThm() =>
            DecideAsync("Copia de temas THM", _settings.Automation.Thm).GetAwaiter().GetResult();
        public Task<bool> ShouldCopyThmAsync() =>
            DecideAsync("Copia de temas THM", _settings.Automation.Thm);
    }
}