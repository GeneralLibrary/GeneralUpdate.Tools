using CommunityToolkit.Mvvm.ComponentModel;

namespace GeneralUpdate.Tools.Models;

public record PlatformItem(int Value, string DisplayName) { public override string ToString() => DisplayName; }
public record AppTypeItem(int Value, string DisplayName) { public override string ToString() => DisplayName; }

public partial class SimulateConfigModel : ObservableObject
{
    [ObservableProperty] private string _appDirectory = string.Empty;
    [ObservableProperty] private string _patchFilePath = string.Empty;
    [ObservableProperty] private string _currentVersion = "1.0.0.0";
    [ObservableProperty] private string _targetVersion = "2.0.0.0";
    [ObservableProperty] private int _platform = 1;
    [ObservableProperty] private int _appType = 1;
    [ObservableProperty] private string _appSecretKey = "dfeb5833-975e-4afb-88f1-6278ee9aeff6";
    [ObservableProperty] private string _productId = "2d974e2a-31e6-4887-9bb1-b4689e98c77a";
    [ObservableProperty] private string _outputDirectory = string.Empty;
    public int ServerPort { get; set; } = 5000;
}
