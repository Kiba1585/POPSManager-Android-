using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using POPSManager.Android.ViewModels;

namespace POPSManager.Android.Views;

public partial class ProcessPopsPage : ContentPage
{
    private string? _lastError;

    public ProcessPopsPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        try
        {
            var vm = serviceProvider.GetService<ProcessPopsViewModel>();
            if (vm == null)
                throw new InvalidOperationException("ProcessPopsViewModel no se pudo crear. Verifica los registros en MauiProgram.");

            BindingContext = vm;
        }
        catch (Exception ex)
        {
            _lastError = ex.ToString();

            // Mostrar el error con botones de acción
            var errorLabel = new Label
            {
                Text = _lastError,
                TextColor = Colors.Red,
                FontSize = 12,
                Margin = new Thickness(0, 10)
            };

            var copyButton = new Button
            {
                Text = "Copiar error",
                BackgroundColor = Colors.Gray
            };
            copyButton.Clicked += async (s, e) =>
            {
                await Clipboard.SetTextAsync(_lastError);
                await DisplayAlert("Copiado", "El error se ha copiado al portapapeles.", "OK");
            };

            var saveButton = new Button
            {
                Text = "Guardar como .txt",
                BackgroundColor = Colors.DarkGray
            };
            saveButton.Clicked += async (s, e) =>
            {
                try
                {
                    var filePath = Path.Combine(FileSystem.AppDataDirectory, "error.txt");
                    await File.WriteAllTextAsync(filePath, _lastError);
                    await DisplayAlert("Guardado", $"Archivo guardado en:\n{filePath}", "OK");
                }
                catch (Exception ioEx)
                {
                    await DisplayAlert("Error al guardar", ioEx.Message, "OK");
                }
            };

            var shareButton = new Button
            {
                Text = "Compartir error",
                BackgroundColor = Colors.Grey
            };
            shareButton.Clicked += async (s, e) =>
            {
                await Share.RequestAsync(new ShareTextRequest
                {
                    Title = "Error en POPSManager",
                    Text = _lastError
                });
            };

            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        Text = "Error al cargar la página de Procesar:",
                        TextColor = Colors.Red,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 18
                    },
                    errorLabel,
                    copyButton,
                    saveButton,
                    shareButton
                }
            };
        }
    }
}