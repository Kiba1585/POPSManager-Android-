using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace POPSManager.Core.Models;

public class GameProgressItem : INotifyPropertyChanged
{
    private string _name = "";
    private string _status = "Pendiente";

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}