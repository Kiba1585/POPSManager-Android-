using System;
using POPSManager.Android.ViewModels;

namespace POPSManager.Android.Views;

public partial class ProcessPopsPage : ContentPage
{
    public ProcessPopsPage(ProcessPopsViewModel vm)
    {
        try
        {
            InitializeComponent();
            BindingContext = vm;
        }
        catch (Exception ex)
        {
            InitializeComponent();
            // Mostrar el error en la página en lugar de cerrar la app
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Children =
                {
                    new Label
                    {
                        Text = "Error al cargar la página de Procesar:",
                        TextColor = Colors.Red,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = ex.ToString(),
                        TextColor = Colors.Red
                    }
                }
            };
        }
    }
}