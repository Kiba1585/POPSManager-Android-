namespace POPSManager.Android;

public partial class App : Application
{
    public App(HomePage home)
    {
        InitializeComponent();
        MainPage = new NavigationPage(home);
    }
}
