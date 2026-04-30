using Microsoft.Maui.Controls;
using POPSManager.Android.ViewModels;

namespace POPSManager.Android.Views;

public partial class ProcessPopsPage : ContentPage
{
    private readonly ProcessPopsViewModel _vm;

    public ProcessPopsPage(ProcessPopsViewModel vm)
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