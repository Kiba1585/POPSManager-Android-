using Microsoft.Maui.Controls;

namespace POPSManager.Android;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Tema oscuro para el Shell
        Shell.SetNavBarBackgroundColor(this, Color.FromArgb("#161B22"));
        Shell.SetNavBarTitleColor(this, Color.FromArgb("#F0F6FC"));
        Shell.SetTabBarBackgroundColor(this, Color.FromArgb("#161B22"));
        Shell.SetTabBarForegroundColor(this, Color.FromArgb("#58A6FF"));
        Shell.SetTabBarUnselectedColor(this, Color.FromArgb("#6E7681"));
    }
}