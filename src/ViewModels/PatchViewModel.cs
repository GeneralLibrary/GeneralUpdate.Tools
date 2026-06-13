using System;
using System.Collections.Generic;
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

public partial class PatchViewModel : ViewModelBase
{
    private readonly DiffService _diff = new();
    private readonly PackageService _pkg = new();
    private readonly EncryptionDetectionService _encScanner = new();
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private readonly AppConfig _config;

    public PatchConfigModel Config { get; } = new();
    [ObservableProperty] private bool _isBuilding;
    [ObservableProperty] private bool _autoUploadEnabled;

    // Progress pipe for real-time result window
    private IProgress<string>? _opProgress;

    public PatchViewModel(AppConfig config)
    {
        _config = config;

        Config.OldDirectory = config.LastPatchOldDir;
        Config.NewDirectory = config.LastPatchNewDir;
        Config.OutputPath = config.LastPatchOutputDir;
        Config.EnableEncryptionCheck = config.EncryptionScanEnabled;
        AutoUploadEnabled = config.AutoUploadEnabled;

        _initialized = true;
    }

    private bool _initialized;

    partial void OnAutoUploadEnabledChanged(bool value)
    {
        if (!_initialized) return;
        _config.AutoUploadEnabled = value;
        ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
    }

    async Task<string?> Pick()
    {
        var tl = Avalonia.Controls.TopLevel.GetTopLevel(
            (Avalonia.Application.Current?.ApplicationLifetime as
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow);
        if (tl == null) return null;
        var r = await tl.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
            { Title = _loc["Patch.Select"], AllowMultiple = false });
        return r.Count > 0 ? r[0].Path.LocalPath : null;
    }

    [RelayCommand]
    async Task SelectOld()
    {
        var p = await Pick();
        if (p != null)
        {
            Config.OldDirectory = p;
            _config.LastPatchOldDir = p;
            ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
        }
    }

    [RelayCommand]
    async Task SelectNew()
    {
        var p = await Pick();
        if (p != null)
        {
            Config.NewDirectory = p;
            _config.LastPatchNewDir = p;
            ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
        }
    }

    [RelayCommand]
    async Task SelectOut()
    {
        var p = await Pick();
        if (p != null)
        {
            Config.OutputPath = p;
            _config.LastPatchOutputDir = p;
            ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
        }
    }

    [RelayCommand]
    async Task Build()
    {
        if (string.IsNullOrWhiteSpace(Config.OldDirectory) || string.IsNullOrWhiteSpace(Config.NewDirectory))
        {
            await DialogHelper.ShowInfoAsync(_loc["Result.ValidationTitle"], _loc["Patch.ValidateDirs"]);
            return;
        }

        if (!SemverValidator.IsValid(Config.Version))
        {
            await DialogHelper.ShowInfoAsync(_loc["Result.ValidationTitle"], _loc.T("Patch.InvalidVersion", Config.Version));
            return;
        }

        _config.EncryptionScanEnabled = Config.EnableEncryptionCheck;
        ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);

        var outDir = string.IsNullOrWhiteSpace(Config.OutputPath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : Config.OutputPath;

        IsBuilding = true;
        try
        {
            await DialogHelper.ShowResultWindowAsync(
                _loc["Patch.Title"],
                async progress =>
                {
                    _opProgress = progress;

                    var tmp = Path.Combine(Path.GetTempPath(), $"gupatch_{DateTime.Now:yyyyMMddHHmmss}");
                    Directory.CreateDirectory(tmp);
                    L(_loc.T("Patch.TempDir", tmp));
                    L(_loc["Patch.Comparing"]);
                    await _diff.GeneratePatchAsync(Config.OldDirectory, Config.NewDirectory, tmp);
                    L(_loc["Patch.PatchDone"]);

                    // Encryption scan
                    if (Config.EnableEncryptionCheck)
                    {
                        L(_loc["Patch.Scanning"]);
                        var scanResult = await _encScanner.ScanDirectoryAsync(tmp);

                        if (scanResult.HasSuspiciousFiles)
                        {
                            L(_loc.T("Patch.ScanFound", scanResult.SuspiciousFiles.Count));
                            var choice = await ShowEncryptionDialogAsync(scanResult);

                            switch (choice)
                            {
                                case EncryptionDialogChoice.Cancel:
                                    L(_loc["Patch.ScanCancelled"]);
                                    Directory.Delete(tmp, true);
                                    return;
                                case EncryptionDialogChoice.SkipSuspicious:
                                    foreach (var f in scanResult.SuspiciousFiles)
                                    {
                                        if (File.Exists(f.FilePath))
                                            File.Delete(f.FilePath);
                                    }
                                    L(_loc.T("Patch.ScanSkipped", scanResult.SuspiciousFiles.Count));
                                    break;
                                case EncryptionDialogChoice.IncludeAll:
                                    break;
                            }
                        }
                        else
                        {
                            L(_loc["Patch.ScanClean"]);
                        }
                    }

                    // Package
                    var name = string.IsNullOrWhiteSpace(Config.PackageName)
                        ? $"patch_{DateTime.Now:yyyyMMddHHmmss}"
                        : Config.PackageName;
                    var zip = Path.Combine(outDir, $"{name}.zip");
                    L(_loc.T("Patch.Packing", Path.GetFileName(zip)));
                    await _pkg.CompressDirectoryAsync(tmp, zip);
                    Directory.Delete(tmp, true);
                    L(_loc.T("Patch.Success", Path.GetFileName(zip), new FileInfo(zip).Length / 1024.0));

                    // Auto-upload
                    if (AutoUploadEnabled)
                    {
                        L(_loc["Upload.Uploading"]);
                        var uploadConfig = new UploadConfig
                        {
                            ServerUrl = _config.UploadServerUrl,
                            UploadEndpoint = _config.UploadEndpoint,
                            TimeoutSeconds = _config.UploadTimeoutSeconds,
                            RetryCount = _config.UploadRetryCount,
                            Auth = _config.UploadAuth,
                            AutoUploadEnabled = true
                        };
                        var uploadSvc = new HttpUploadService();
                        var result = await uploadSvc.UploadAsync(zip, uploadConfig);
                        if (result.Success)
                            L(_loc["Upload.Success"]);
                        else
                            L(_loc.T("Upload.Failed", result.ErrorMessage ?? "Unknown error"));
                    }
                },
                outDir);
        }
        finally
        {
            _opProgress = null;
            IsBuilding = false;
        }
    }

    void L(string m)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {m}";
        _opProgress?.Report(line);
    }

    // ── Encryption scan dialog ──────────────────────────────

    private enum EncryptionDialogChoice { Cancel, SkipSuspicious, IncludeAll }

    private static async Task<EncryptionDialogChoice> ShowEncryptionDialogAsync(EncryptionScanResult result)
    {
        var loc = LocalizationService.Instance;
        var owner = (Avalonia.Application.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        var tcs = new TaskCompletionSource<EncryptionDialogChoice>();

        var dialog = new Avalonia.Controls.Window
        {
            Title = loc["Patch.ScanDialogTitle"],
            Width = 600,
            Height = 480,
            WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
            CanResize = true,
            MinWidth = 520,
            MinHeight = 380
        };

        var rootPanel = new Avalonia.Controls.StackPanel { Margin = new Avalonia.Thickness(20, 16), Spacing = 10 };

        rootPanel.Children.Add(new Avalonia.Controls.TextBlock
        {
            Text = loc["Patch.ScanDialogHeader"],
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 13
        });

        var highRisk = result.SuspiciousFiles.Where(f => f.Level == RiskLevel.High).ToList();
        var mediumRisk = result.SuspiciousFiles.Where(f => f.Level == RiskLevel.Medium).ToList();
        var lowRisk = result.SuspiciousFiles.Where(f => f.Level == RiskLevel.Low).ToList();

        var listPanel = new Avalonia.Controls.StackPanel { Spacing = 4 };
        AddRiskGroup(listPanel, loc["Patch.ScanHighRisk"], highRisk, true);
        AddRiskGroup(listPanel, loc["Patch.ScanMediumRisk"], mediumRisk, false);
        AddRiskGroup(listPanel, loc["Patch.ScanLowRisk"], lowRisk, false);

        rootPanel.Children.Add(new Avalonia.Controls.ScrollViewer
        {
            Content = listPanel,
            MaxHeight = 250,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        });

        rootPanel.Children.Add(new Avalonia.Controls.TextBlock
        {
            Text = loc.T("Patch.ScanFilesScanned", result.TotalFilesScanned),
            FontSize = 12,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Gray)
        });

        var btnPanel = new Avalonia.Controls.StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        var btnSkip = new Avalonia.Controls.Button { Content = loc["Patch.ScanBtnSkip"], MinWidth = 170 };
        var btnInclude = new Avalonia.Controls.Button { Content = loc["Patch.ScanBtnInclude"], MinWidth = 130 };
        var btnCancel = new Avalonia.Controls.Button { Content = loc["Patch.ScanBtnCancel"], MinWidth = 70 };

        btnSkip.Click += (_, _) => { tcs.TrySetResult(EncryptionDialogChoice.SkipSuspicious); dialog.Close(); };
        btnInclude.Click += (_, _) => { tcs.TrySetResult(EncryptionDialogChoice.IncludeAll); dialog.Close(); };
        btnCancel.Click += (_, _) => { tcs.TrySetResult(EncryptionDialogChoice.Cancel); dialog.Close(); };
        dialog.Closed += (_, _) => tcs.TrySetResult(EncryptionDialogChoice.Cancel);

        btnPanel.Children.Add(btnSkip);
        btnPanel.Children.Add(btnInclude);
        btnPanel.Children.Add(btnCancel);
        rootPanel.Children.Add(btnPanel);

        dialog.Content = rootPanel;

        if (owner != null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        return await tcs.Task;
    }

    private static void AddRiskGroup(Avalonia.Controls.StackPanel parent, string title,
        List<SuspiciousFile> files, bool isHighRisk)
    {
        if (files.Count == 0) return;
        parent.Children.Add(new Avalonia.Controls.TextBlock
        {
            Text = $"{title} ({files.Count})",
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            FontSize = 13,
            Margin = new Avalonia.Thickness(0, 4, 0, 2)
        });
        foreach (var f in files)
        {
            var line = new Avalonia.Controls.TextBlock
            {
                Text = FormatSuspiciousFileLine(f),
                FontSize = 11,
                FontFamily = "Consolas,monospace",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(12, 1, 0, 1)
            };
            if (isHighRisk)
                line.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.DarkRed);
            parent.Children.Add(line);
        }
    }

    private static string FormatSuspiciousFileLine(SuspiciousFile f)
    {
        var detail = string.IsNullOrEmpty(f.DetectionDetail) ? "" : $"  [{f.DetectionDetail}]";
        return $"{f.RelativePath}  — {f.Reason}{detail}";
    }
}
