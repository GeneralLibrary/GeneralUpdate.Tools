using CommunityToolkit.Mvvm.ComponentModel;

namespace GeneralUpdate.Tools.V12.Models;

public partial class ExtensionConfigModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _version = "1.0.0.0";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _extensionDirectory = "";
    [ObservableProperty] private string _exportPath = "";
    [ObservableProperty] private string _dependencies = "";
    [ObservableProperty] private string _publisher = "";
    [ObservableProperty] private string _license = "";
    [ObservableProperty] private string _categoriesText = "";
    [ObservableProperty] private string _minHostVersion = "";
    [ObservableProperty] private string _maxHostVersion = "";
    [ObservableProperty] private int _platformValue = 1;
    [ObservableProperty] private bool _isPreRelease;
    [ObservableProperty] private string _outputPath = "";
}
