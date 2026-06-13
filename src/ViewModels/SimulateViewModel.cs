using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.Configuration;
using GeneralUpdate.Tools.Models;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.ViewModels;

public partial class SimulateViewModel : ViewModelBase
{
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private readonly SimulationService _sim = new();
    private readonly ReportGeneratorService _report = new();
    private readonly AppConfig _config;

    public SimulateConfigModel Config { get; } = new();

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _status;
    [ObservableProperty] private string _startButtonText;
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

    public SimulateViewModel(AppConfig config)
    {
        _config = config;

        // Restore path memory
        Config.AppDirectory = config.LastSimulateAppDir;
        Config.PatchFilePath = config.LastSimulatePatchFile;

        // Restore simulation config
        Config.ServerPort = int.TryParse(config.SimulationServerPort, out var port) ? port : 5000;
        Config.Platform = config.SimulationPlatformType == "Linux" ? 2 : 1;
        Config.AppType = config.SimulationAppType == "UpgradeApp" ? 2 : 1;

        _status = _loc["Patch.Ready"];
        _startButtonText = _loc["Sim.Start"];
    }

    public int PlatformIndex
    {
        get => Config.Platform == 2 ? 1 : 0;
        set => Config.Platform = value == 1 ? 2 : 1;
    }

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

    [RelayCommand]
    async Task SelectAppDir()
    {
        var p = await PickFolder(_loc["Sim.SelectAppDir"]);
        if (p != null)
        {
            Config.AppDirectory = p;
            _config.LastSimulateAppDir = p;
            ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
        }
    }

    [RelayCommand]
    async Task SelectPatch()
    {
        var p = await PickFile(_loc["Sim.SelectPatch"]);
        if (p != null)
        {
            Config.PatchFilePath = p;
            _config.LastSimulatePatchFile = p;
            ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
        }
    }

    [RelayCommand]
    async Task StartSimulation()
    {
        if (string.IsNullOrWhiteSpace(Config.AppDirectory)) { return; }
        if (string.IsNullOrWhiteSpace(Config.PatchFilePath)) { return; }
        if (!SemverValidator.IsValid(Config.CurrentVersion)) return;
        if (!SemverValidator.IsValid(Config.TargetVersion)) return;

        // Persist simulation settings
        _config.SimulationServerPort = Config.ServerPort.ToString();
        _config.SimulationPlatformType = Config.Platform == 2 ? "Linux" : "Windows";
        _config.SimulationAppType = Config.AppType == 2 ? "UpgradeApp" : "ClientApp";
        ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);

        IsRunning = true;
        StartButtonText = "⏳ Running...";
        try
        {
            await DialogHelper.ShowResultWindowAsync(
                _loc["Sim.Title"],
                async progress =>
                {
                    var result = await _sim.RunAsync(Config, progress);
                    if (result.Success)
                        progress.Report(_loc.T("Sim.Completed", result.Elapsed.TotalSeconds));
                    else
                        progress.Report(_loc.T("Sim.Failed", result.ErrorMessage ?? "unknown"));
                    progress.Report($"Result: {(result.Success ? "PASS" : "FAIL")}");
                    foreach (var note in result.Notes) progress.Report($"  Note: {note}");
                    var reportPath = await _report.GenerateAsync(Config, result, Config.AppDirectory);
                    progress.Report(_loc.T("Sim.Report", reportPath));
                },
                Config.AppDirectory);
        }
        finally { IsRunning = false; StartButtonText = _loc["Sim.Start"]; }
    }
}
