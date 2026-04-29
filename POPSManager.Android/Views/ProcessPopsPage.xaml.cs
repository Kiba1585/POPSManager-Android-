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
            BindingContext = null;
            Content = new Label
            {
                Text = $"Error al cargar: {ex.Message}",
                TextColor = Colors.Red,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
        }
    }
}