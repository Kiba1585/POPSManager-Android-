namespace POPSManager.Core.Settings;

public enum CheatMode
{
    Disabled,
    AutoForPal,
    AskEachTime,
    ManualSelection
}

public class CheatSettings
{
    public CheatMode Mode { get; set; } = CheatMode.AutoForPal;
    public bool UseAutoGameFixes { get; set; } = true;
    public bool UseEngineFixes { get; set; } = true;
    public bool UseHeuristicFixes { get; set; } = true;
    public bool UseDatabaseFixes { get; set; } = true;
    public bool EnableCustomCheats { get; set; } = true;
}