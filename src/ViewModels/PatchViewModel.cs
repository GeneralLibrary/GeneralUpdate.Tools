using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.Models;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.ViewModels;

public partial class PatchViewModel : ViewModelBase
{
    private readonly DiffService _diff = new();
    private readonly PackageService _pkg = new();
    private readonly LocalizationService _loc = LocalizationService.Instance;

    public PatchConfigModel Config { get; } = new();
    [ObservableProperty] private bool _isBuilding;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _status;
    [ObservableProperty] private ObservableCollection<string> _log = new();

    public PatchViewModel()
    {
        _status = _loc["Patch.Ready"];
    }

    async Task<string?> Pick()
    {
        var tl = Avalonia.Controls.TopLevel.GetTopLevel((Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow);
        if (tl == null) return null;
        var r = await tl.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = _loc["Patch.Select"], AllowMultiple = false });
        return r.Count > 0 ? r[0].Path.LocalPath : null;
    }

    [RelayCommand] async Task SelectOld() { var p = await Pick(); if (p != null) { Config.OldDirectory = p; L(_loc.T("Patch.OldSelected", p)); } }
    [RelayCommand] async Task SelectNew() { var p = await Pick(); if (p != null) { Config.NewDirectory = p; L(_loc.T("Patch.NewSelected", p)); } }
    [RelayCommand] async Task SelectOut() { var p = await Pick(); if (p != null) Config.OutputPath = p; }

    [RelayCommand] async Task Build()
    {
        if (string.IsNullOrWhiteSpace(Config.OldDirectory) || string.IsNullOrWhiteSpace(Config.NewDirectory)) { Status = _loc["Patch.ValidateDirs"]; return; }
        IsBuilding = true; Log.Clear(); Progress = 0; Status = _loc["Patch.Building"];
        try
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"gupatch_{DateTime.Now:yyyyMMddHHmmss}"); Directory.CreateDirectory(tmp);
            L(_loc.T("Patch.TempDir", tmp)); Progress = 20;
            L(_loc["Patch.Comparing"]);
            await _diff.GeneratePatchAsync(Config.OldDirectory, Config.NewDirectory, tmp);
            Progress = 70; L(_loc["Patch.PatchDone"]);
            var outDir = string.IsNullOrWhiteSpace(Config.OutputPath) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : Config.OutputPath;
            var name = string.IsNullOrWhiteSpace(Config.PackageName) ? $"patch_{DateTime.Now:yyyyMMddHHmmss}" : Config.PackageName;
            var zip = Path.Combine(outDir, $"{name}.zip");
            L(_loc.T("Patch.Packing", Path.GetFileName(zip)));
            await _pkg.CompressDirectoryAsync(tmp, zip);
            Directory.Delete(tmp, true);
            Progress = 100;
            Status = _loc.T("Patch.Success", Path.GetFileName(zip), new FileInfo(zip).Length / 1024.0);
            L(Status);
        }
        catch (Exception ex) { Status = _loc.T("Patch.Failed", ex.Message); L(_loc.T("Patch.Error", ex)); }
        finally { IsBuilding = false; }
    }
    void L(string m) => Log.Add($"[{DateTime.Now:HH:mm:ss}] {m}");
}
