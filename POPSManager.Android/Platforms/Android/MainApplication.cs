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
            System.Diagnostics.Debug.WriteLine("Error en MainApplication.OnCreate: " + ex.ToString());
            try
            {
                new AlertDialog.Builder(this)
                    .SetTitle("Error")
                    .SetMessage(ex.Message)
                    .SetPositiveButton("OK", (sender, args) => { })
                    .Show();
            }
            catch { /* evitar crash si el contexto no permite diálogos */ }
        }
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}