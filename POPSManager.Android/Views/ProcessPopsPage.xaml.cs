using POPSManager.Android.ViewModels;

namespace POPSManager.Android.Views;

public partial class ProcessPopsPage : ContentPage
{
    public ProcessPopsPage(ProcessPopsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}