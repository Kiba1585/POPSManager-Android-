public override void OnCreate()
{
    try
    {
        base.OnCreate();
    }
    catch (Exception ex)
    {
        // Mostrar un diálogo o escribir en log
        System.Diagnostics.Debug.WriteLine("Error en OnCreate: " + ex.ToString());
        // Opcional: mostrar un AlertDialog nativo con el mensaje
        new AlertDialog.Builder(this)
            .SetTitle("Error")
            .SetMessage(ex.Message)
            .SetPositiveButton("OK", (sender, args) => { })
            .Show();
    }
}