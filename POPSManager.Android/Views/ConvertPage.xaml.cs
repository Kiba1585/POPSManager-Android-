using POPSManager.Android.ViewModels;

namespace POPSManager.Android.Views;

public partial class ConvertPage : ContentPage
{
    private readonly ConvertViewModel _vm;

    public ConvertPage(ConvertViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.RefreshFromSettings();
    }
}