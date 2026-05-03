namespace POPSManager.Android.Models
{
    public class GameItem
    {
        public string GameId { get; set; } = "";
        public string OriginalGameId { get; set; } = "";
        public string Name { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string GameFolder { get; set; } = "";
        public bool IsMultiDisc { get; set; }
        public int DiscNumber { get; set; } = 1;
        public override string ToString() => Name;
    }
}