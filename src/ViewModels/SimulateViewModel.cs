using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

    /// <summary>
    /// Maps Config.Platform (int) to Platforms collection index.
    /// 1 (Windows) → 0, 2 (Linux) → 1.
    /// </summary>
    public int PlatformIndex
    {
        get => Config.Platform == 2 ? 1 : 0;
        set => Config.Platform = value == 1 ? 2 : 1;
    }

    /// <summary>
    /// Maps Config.AppType (int) to AppTypes collection index.
    /// 1 (ClientApp) → 0, 2 (UpgradeApp) → 1.
    /// </summary>
    public int AppTypeIndex
    {
        get => Config.AppType == 2 ? 1 : 0;
        set => Config.AppType = value == 1 ? 2 : 1;
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

    [RelayCommand] async Task SelectAppDir() { var p = await PickFolder(_loc["Sim.SelectAppDir"]); if (p != null) Config.AppDirectory = p; }
    [RelayCommand] async Task SelectPatch() { var p = await PickFile(_loc["Sim.SelectPatch"]); if (p != null) Config.PatchFilePath = p; }
    [RelayCommand] async Task SelectOutputDir() { var p = await PickFolder(_loc["Sim.SelectOutput"]); if (p != null) Config.OutputDirectory = p; }

    [RelayCommand]
    async Task StartServer()
    {
        if (string.IsNullOrWhiteSpace(Config.AppDirectory)) { Status = _loc["Sim.ValidateDirs"]; return; }
        if (string.IsNullOrWhiteSpace(Config.PatchFilePath)) { Status = _loc["Sim.ValidateDirs"]; return; }
        if (string.IsNullOrWhiteSpace(Config.OutputDirectory)) { Status = _loc["Sim.ValidateDirs"]; return; }

        IsRunning = true; Log.Clear(); Status = _loc["Sim.Starting"];
        try
        {
            await _sim.StartServerAsync(Config, new Progress<string>(L));
            Status = $"Server: {_sim.ServerBaseUrl}";
            L($"Server running on {_sim.ServerBaseUrl}");
            L($"Manual: dotnet script client.csx");
        }
        catch (Exception ex) { Status = $"Error: {ex.Message}"; L($"FATAL: {ex}"); }
        finally { IsRunning = false; }
    }

    [RelayCommand]
    async Task StopServer()
    {
        await _sim.StopServerAsync();
        Status = _loc["Patch.Ready"];
        L("Server stopped");
    }

    [RelayCommand]
    async Task RunClient()
    {
        if (!Config.ServerRunning) { Status = "Server not running"; return; }
        IsRunning = true; Status = "Running client...";
        try
        {
            var result = await _sim.RunClientAsync(Config, new Progress<string>(L));
            if (result.Success)
            {
                Status = _loc.T("Sim.Completed", result.Elapsed.TotalSeconds);
                var reportPath = await _report.GenerateAsync(Config, result, Config.OutputDirectory);
                L(_loc.T("Sim.Report", reportPath));
            }
            else { Status = _loc.T("Sim.Failed", result.ErrorMessage ?? "unknown"); }
        }
        catch (Exception ex) { Status = $"Error: {ex.Message}"; L($"FATAL: {ex}"); }
        finally { IsRunning = false; }
    }

    void L(string msg) => Log.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
}
