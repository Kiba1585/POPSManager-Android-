using POPSManager.Android.ViewModels;

namespace POPSManager.Android.Views;

public partial class ConvertPage : ContentPage
{
    public ConvertPage(ConvertViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}