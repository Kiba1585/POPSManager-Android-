using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Provider;

namespace POPSManager.Android;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize |
                           ConfigChanges.Orientation |
                           ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize |
                           ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Solicitar permiso de gestión de almacenamiento en Android 11+
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            if (!Android.OS.Environment.IsExternalStorageManager)
            {
                var intent = new Android.Content.Intent(
                    Settings.ActionManageAllFilesAccessPermission);
                StartActivity(intent);
            }
        }
    }
}