namespace POPSManager.Core.Models;

public class GameEntry
{
    public string GameId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Region { get; set; } = "";
    public string Publisher { get; set; } = "";
    public int Year { get; set; }
    public int DiscCount { get; set; }
    public string[]? DiscNames { get; set; }
    public string? CoverUrl { get; set; }
    public string[]? Tags { get; set; }
    public string[]? CheatFixes { get; set; }
    public string[]? GraphicsFixes { get; set; }
    public string[]? VideoFixes { get; set; }
    public string[]? SoundFixes { get; set; }
    public bool HasFmvIssues { get; set; }
    public bool HasTimingIssues { get; set; }
    public bool RequiresPal60 { get; set; }
    public bool RequiresSkipVideos { get; set; }
    public bool RequiresFixCdda { get; set; }
    public bool RequiresFixGraphics { get; set; }
    public bool RequiresFixSound { get; set; }
}