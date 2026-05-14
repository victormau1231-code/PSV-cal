using System.ComponentModel;
using System.Runtime.CompilerServices;
using PSVCalc.Core.Enums;
using PSVCalc.Core.Services;

namespace PSVCalc.App.ViewModels;

public sealed class LocalizationProvider : INotifyPropertyChanged
{
    private UiLanguage _language = UiLanguage.ZhCn;

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiLanguage Language
    {
        get => _language;
        set
        {
            if (_language == value)
            {
                return;
            }

            _language = value;
            OnPropertyChanged("Item[]");
        }
    }

    public string this[string key] => LocalizationCatalog.Get(Language, key);

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

