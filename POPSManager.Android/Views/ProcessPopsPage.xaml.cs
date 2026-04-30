using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using POPSManager.Android.ViewModels;

namespace POPSManager.Android.Views;

public partial class ProcessPopsPage : ContentPage
{
    private ProcessPopsViewModel? _vm;
    private string? _lastError;

    public ProcessPopsPage(ProcessPopsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm?.RefreshFromSettings();
    }
}