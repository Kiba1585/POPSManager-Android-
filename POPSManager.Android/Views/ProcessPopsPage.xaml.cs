using System;
using Microsoft.Maui.Controls;
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
            // Si el ViewModel es nulo o falla la asignación, mostramos el error
            InitializeComponent(); // Aseguramos que se inicialice la vista
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Children =
                {
                    new Label
                    {
                        Text = "Error al cargar Procesar:",
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