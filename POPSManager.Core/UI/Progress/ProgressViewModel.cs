using System.Collections.ObjectModel;

namespace POPSManager.Core.UI.Progress
{
    public class ProgressViewModel
    {
        public ObservableCollection<object> Items { get; } = new();

        public void AddGame(string name, string id) { }
        public void UpdateStatus(string id, string status) { }
        public void MarkCompleted(string id) { }
        public void MarkError(string id, string error) { }
    }
}
