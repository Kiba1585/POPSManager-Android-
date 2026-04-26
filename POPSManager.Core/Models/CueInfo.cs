using System.Collections.Generic;

namespace POPSManager.Core.Models;

public class CueInfo
{
    public string CuePath { get; set; } = "";
    public List<CueFile> Files { get; set; } = new();
    public List<CueTrack> Tracks { get; set; } = new();
    public string BinPath { get; set; } = "";
}

public class CueFile
{
    public string Path { get; set; } = "";
    public string Type { get; set; } = "";
}

public class CueTrack
{
    public int Number { get; set; }
    public string Mode { get; set; } = "";
    public List<CueIndex> Indexes { get; set; } = new();
    public CueFile? File { get; set; }
}

public class CueIndex
{
    public int Number { get; set; }
    public int Minute { get; set; }
    public int Second { get; set; }
    public int Frame { get; set; }
    public int ToSector() => (Minute * 60 * 75) + (Second * 75) + Frame;
}