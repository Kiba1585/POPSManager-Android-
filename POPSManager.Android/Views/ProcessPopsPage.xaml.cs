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
    private readonly ProcessPopsViewModel _vm;
    private string? _lastError;

    public ProcessPopsPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        try
        {
            _vm = serviceProvider.GetService<ProcessPopsViewModel>()
                  ?? throw new InvalidOperationException("ProcessPopsViewModel no se pudo crear.");
            BindingContext = _vm;
        }
        catch (Exception ex)
        {
            _lastError = ex.ToString();
            MostrarError();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm?.RefreshFromSettings();
    }

    private void MostrarError()
    {
        var errorEditor = new Editor
        {
            Text = _lastError,
            TextColor = Colors.Red,
            FontSize = 12,
            IsReadOnly = true,
            AutoSize = EditorAutoSizeOption.TextChanges,
            HeightRequest = 200
        };
        // ... resto del código de botones igual que antes ...
        Content = new VerticalStackLayout
        {
            Padding = 20,
            Spacing = 10,
            Children = { /* labels, errorEditor, botones */ }
        };
    }
}