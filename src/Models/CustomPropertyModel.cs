using CommunityToolkit.Mvvm.ComponentModel;

namespace GeneralUpdate.Tool.Avalonia.Models;

public class CustomPropertyModel : ObservableObject
{
    private string _key;
    private string _value;

    /// <summary>
    /// Property key
    /// </summary>
    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    /// <summary>
    /// Property value
    /// </summary>
    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}