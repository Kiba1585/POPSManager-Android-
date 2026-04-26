using POPSManager.Core.Logic.Automation;

namespace POPSManager.Core.Settings;

public class AutomationSettings
{
    public AutomationMode Mode { get; set; } = AutomationMode.Asistido;
    public AutoBehavior Conversion { get; set; } = AutoBehavior.Automatico;
    public AutoBehavior MultiDisc { get; set; } = AutoBehavior.Automatico;
    public AutoBehavior FolderCreation { get; set; } = AutoBehavior.Automatico;
    public AutoBehavior ElfGeneration { get; set; } = AutoBehavior.Automatico;
    public AutoBehavior Covers { get; set; } = AutoBehavior.Preguntar;
    public AutoBehavior Database { get; set; } = AutoBehavior.Automatico;
    public AutoBehavior Cheats { get; set; } = AutoBehavior.Preguntar;
    public AutoBehavior Metadata { get; set; } = AutoBehavior.Automatico;
    public AutoBehavior Notifications { get; set; } = AutoBehavior.Automatico;
    public AutoBehavior Lng { get; set; } = AutoBehavior.Automatico;
    public AutoBehavior Thm { get; set; } = AutoBehavior.Automatico;
}