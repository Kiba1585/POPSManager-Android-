namespace POPSManager.Android;

public partial class App : Application
{
    public App(POPSManager.Android.Views.HomePage home)
    {
        InitializeComponent();
        MainPage = new NavigationPage(home);
    }
}