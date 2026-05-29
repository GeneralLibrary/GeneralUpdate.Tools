using CommunityToolkit.Mvvm.ComponentModel;

namespace GeneralUpdate.Tools.Models;

public partial class ConfigGeneratorModel : ObservableObject
{
    // ── File picker paths ──
    [ObservableProperty] private string _clientPath = "";
    [ObservableProperty] private string _upgradePath = "";

    // ── Analysis state ──
    [ObservableProperty] private bool _isAnalyzed;
    [ObservableProperty] private bool _isAnalyzing;

    // ── Editable fields (auto-filled + user input) ──
    [ObservableProperty] private string _mainAppName = "";
    [ObservableProperty] private string _clientVersion = "";
    [ObservableProperty] private string _updateAppName = "Update.exe";
    [ObservableProperty] private string _upgradeClientVersion = "";
    [ObservableProperty] private string _appType = "Client";
    [ObservableProperty] private string _productId = "";
    [ObservableProperty] private string _updatePath = "update/";

    // ── Info text ──
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _clientFramework = "";
    [ObservableProperty] private string _upgradeFramework = "";

    // ── Checkboxes ──
    [ObservableProperty] private bool _openManifestDir = true;
    [ObservableProperty] private bool _openSampleDir = true;

    // ── Generated JSON preview ──
    [ObservableProperty] private string _previewJson = "";
}
