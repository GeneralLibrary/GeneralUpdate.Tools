using CommunityToolkit.Mvvm.ComponentModel;

namespace GeneralUpdate.Tools.V12.Models;

public partial class PatchConfigModel : ObservableObject
{
    [ObservableProperty] private string _oldDirectory = "";
    [ObservableProperty] private string _newDirectory = "";
    [ObservableProperty] private string _packageName = "";
    [ObservableProperty] private string _version = "1.0.0.0";
    [ObservableProperty] private string _format = ".zip";
    [ObservableProperty] private string _outputPath = "";
}
