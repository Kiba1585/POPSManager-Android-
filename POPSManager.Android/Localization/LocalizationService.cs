using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace POPSManager.Core.Localization;

public enum AppLanguage
{
    Auto, Spanish, English, French, German, Italian, Portuguese, Japanese
}

public class LocalizationService : INotifyPropertyChanged
{
    private readonly ResourceManager _resourceManager;
    private AppLanguage _language = AppLanguage.Auto;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LocalizationService()
    {
        _resourceManager = new ResourceManager("POPSManager.Core.Localization.Strings", typeof(LocalizationService).Assembly);
    }

    public AppLanguage Language
    {
        get => _language;
        set
        {
            if (_language != value)
            {
                _language = value;
                OnPropertyChanged(nameof(Language));
                OnPropertyChanged(nameof(CurrentLanguage));
            }
        }
    }

    public string CurrentLanguage => Language switch
    {
        AppLanguage.Spanish => "es",
        AppLanguage.English => "en",
        AppLanguage.French => "fr",
        AppLanguage.German => "de",
        AppLanguage.Italian => "it",
        AppLanguage.Portuguese => "pt",
        AppLanguage.Japanese => "ja",
        AppLanguage.Auto => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
        _ => "en"
    };

    public string GetString(string key)
    {
        try
        {
            var culture = new CultureInfo(CurrentLanguage);
            return _resourceManager.GetString(key, culture) ?? key;
        }
        catch
        {
            return key;
        }
    }

    public string GetString(string key, params object[] args)
    {
        string format = GetString(key);
        return args.Length > 0 ? string.Format(format, args) : format;
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(CurrentLanguage));
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}