using CommunityToolkit.Mvvm.ComponentModel;

namespace GeneralUpdate.Tools.Models;

public partial class MobilePackageModel : ObservableObject
{
    // ── Mode ───────────────────────────────────────────────
    // true = project mode (.csproj), false = file mode (.apk/.aab)
    [ObservableProperty] private bool _useProjectMode = true;

    // ── File mode ──────────────────────────────────────────
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private PackageFormat _format = PackageFormat.Unknown;
    [ObservableProperty] private string _formatDisplay = "";

    // ── Project mode ───────────────────────────────────────
    [ObservableProperty] private string _projectPath = "";
    [ObservableProperty] private string _projectType = ""; // "MAUI" / "Avalonia" / ".NET Android"
    [ObservableProperty] private string _projectBuildOutput = ""; // path to published APK/AAB

    // ── Metadata (auto-detected, editable) ─────────────────
    [ObservableProperty] private string _packageName = "";
    [ObservableProperty] private string _versionName = "";
    [ObservableProperty] private string _versionCode = "";

    // ── Auto-computed (read-only display) ─────────────────
    [ObservableProperty] private string _sha256Hash = "";
    [ObservableProperty] private long _fileSize;
    [ObservableProperty] private string _fileSizeDisplay = "";

    // ── Upload config ──────────────────────────────────────
    [ObservableProperty] private int _platform = 4; // default Android
    // 0-based ComboBox index → Server value = Index + 1 (1=Client, 2=Upgrade)
    [ObservableProperty] private int _appType;      // 0=Client, 1=Upgrade
    [ObservableProperty] private string _productId = "";
    [ObservableProperty] private string _productName = "";
    [ObservableProperty] private string _releaseNotes = "";
    [ObservableProperty] private bool _isForcibly;
    [ObservableProperty] private string _outputDirectory = "";
}
