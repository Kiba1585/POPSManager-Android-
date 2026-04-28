using Android.App;
using Android.Runtime;
using System;

namespace POPSManager.Android;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        try
        {
            base.OnCreate();
        }
        catch (Exception ex)
        {
            // Log del error
            System.Diagnostics.Debug.WriteLine("Error en MainApplication.OnCreate: " + ex.ToString());
            // Opcional: mostrar un diálogo nativo
            new AlertDialog.Builder(this)
                .SetTitle("Error")
                .SetMessage(ex.Message)
                .SetPositiveButton("OK", (sender, args) => { })
                .Show();
        }
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}