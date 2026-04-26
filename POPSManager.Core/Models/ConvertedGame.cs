namespace POPSManager.Core.Models;

public class ConvertedGame
{
    public required string VcdPath { get; set; }
    public required string BaseName { get; set; }
    public int DiscNumber { get; set; }
    public bool IsMultiDisc { get; set; }
}