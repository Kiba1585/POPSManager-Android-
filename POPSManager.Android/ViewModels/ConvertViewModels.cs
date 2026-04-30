// Dentro de ConvertViewModel, al final de la clase:

public void RefreshFromSettings()
{
    var savedSource = _settings.SourceFolder;
    if (!string.IsNullOrEmpty(savedSource) && savedSource != _sourceFolder)
    {
        _sourceFolder = savedSource;
        OnPropertyChanged(nameof(SourceFolder));
        LoadFiles();
    }

    var savedDest = _settings.DestinationFolder;
    if (!string.IsNullOrEmpty(savedDest) && savedDest != _destFolder)
    {
        _destFolder = savedDest;
        OnPropertyChanged(nameof(DestFolder));
    }
}