using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.Models;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.ViewModels;

public partial class SimulateViewModel : ViewModelBase
{
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private readonly SimulationService _sim = new();
    private readonly ReportGeneratorService _report = new();

    public SimulateConfigModel Config { get; } = new();

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _status;
    [ObservableProperty] private ObservableCollection<string> _log = new();

    public ObservableCollection<PlatformItem> Platforms { get; } = new()
    {
        new(1, "Windows"),
        new(2, "Linux")
    };

    public ObservableCollection<AppTypeItem> AppTypes { get; } = new()
    {
        new(1, "ClientApp"),
        new(2, "UpgradeApp")
    };

    public SimulateViewModel()
    {
        _status = _loc["Patch.Ready"];
    }

    async Task<string?> PickFolder(string title)
    {
        var tl = Avalonia.Controls.TopLevel.GetTopLevel(
            (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow);
        if (tl == null) return null;
        var r = await tl.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = title, AllowMultiple = false });
        return r.Count > 0 ? r[0].Path.LocalPath : null;
    }

    async Task<string?> PickFile(string title)
    {
        var tl = Avalonia.Controls.TopLevel.GetTopLevel(
            (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow);
        if (tl == null) return null;
        var r = await tl.StorageProvider.OpenFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerOpenOptions { Title = title, AllowMultiple = false });
        return r.Count > 0 ? r[0].Path.LocalPath : null;
    }

    [RelayCommand] async Task SelectAppDir() { var p = await PickFolder("选择旧版本应用目录"); if (p != null) Config.AppDirectory = p; }
    [RelayCommand] async Task SelectPatch() { var p = await PickFile("选择补丁包"); if (p != null) Config.PatchFilePath = p; }
    [RelayCommand] async Task SelectOutputDir() { var p = await PickFolder("选择模拟输出目录"); if (p != null) Config.OutputDirectory = p; }

    [RelayCommand]
    async Task StartSimulation()
    {
        if (string.IsNullOrWhiteSpace(Config.AppDirectory)) { Status = _loc["Sim.ValidateDirs"]; return; }
        if (string.IsNullOrWhiteSpace(Config.PatchFilePath)) { Status = _loc["Sim.ValidateDirs"]; return; }
        if (string.IsNullOrWhiteSpace(Config.OutputDirectory)) { Status = _loc["Sim.ValidateDirs"]; return; }

        IsRunning = true;
        Log.Clear();
        Status = "Starting simulation...";

        try
        {
            var progress = new Progress<string>(L);
            var result = await _sim.RunAsync(Config, progress);

            if (result.Success)
            {
                Status = $"Simulation completed ({result.Elapsed.TotalSeconds:F1}s)";
                L($"Result: {(result.Success ? "PASS" : "FAIL")}");
                foreach (var note in result.Notes)
                    L($"  Note: {note}");

                // Generate report
                var reportPath = await _report.GenerateAsync(Config, result, Config.OutputDirectory);
                L($"Report: {reportPath}");
            }
            else
            {
                Status = $"Simulation failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            L($"FATAL: {ex}");
        }
        finally
        {
            IsRunning = false;
        }
    }

    void L(string msg) => Log.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
}

public record PlatformItem(int Value, string DisplayName) { public override string ToString() => DisplayName; }
public record AppTypeItem(int Value, string DisplayName) { public override string ToString() => DisplayName; }
