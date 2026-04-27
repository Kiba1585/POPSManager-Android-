namespace POPSManager.Core.Logic.Cheats
{
    public class CheatDefinition
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public bool IsOfficial { get; set; }
        public bool IsUserDefined { get; set; }
    }
}