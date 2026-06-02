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
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _status;
    [ObservableProperty] private ObservableCollection<string> _log = new();

    // Upload-related properties
    [ObservableProperty] private bool _autoUploadEnabled;
    [ObservableProperty] private string _uploadStatus = string.Empty;
    [ObservableProperty] private double _uploadProgress;

    public PatchViewModel(AppConfig config)
    {
        _config = config;

        // Restore path memory
        Config.OldDirectory = config.LastPatchOldDir;
        Config.NewDirectory = config.LastPatchNewDir;
        Config.OutputPath = config.LastPatchOutputDir;
        Config.EnableEncryptionCheck = config.EncryptionScanEnabled;
        AutoUploadEnabled = config.AutoUploadEnabled;

        _status = _loc["Patch.Ready"];
    }

    /// <summary>Persist auto-upload toggle changes immediately.</summary>
    partial void OnAutoUploadEnabledChanged(bool value)
    {
        _config.AutoUploadEnabled = value;
        _ = ConfigServiceSingleton.Instance.SaveAsync();
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
            _ = ConfigServiceSingleton.Instance.SaveAsync();
            L(_loc.T("Patch.OldSelected", p));
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
            _ = ConfigServiceSingleton.Instance.SaveAsync();
            L(_loc.T("Patch.NewSelected", p));
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
            _ = ConfigServiceSingleton.Instance.SaveAsync();
        }
    }

    [RelayCommand]
    async Task Build()
    {
        if (string.IsNullOrWhiteSpace(Config.OldDirectory) || string.IsNullOrWhiteSpace(Config.NewDirectory))
        {
            Status = _loc["Patch.ValidateDirs"];
            return;
        }

        if (!SemverValidator.IsValid(Config.Version))
        {
            Status = _loc.T("Patch.InvalidVersion", Config.Version);
            return;
        }

        // Persist encryption scan preference
        _config.EncryptionScanEnabled = Config.EnableEncryptionCheck;
        _ = ConfigServiceSingleton.Instance.SaveAsync();

        IsBuilding = true;
        Log.Clear();
        Progress = 0;
        UploadProgress = 0;
        UploadStatus = string.Empty;
        Status = _loc["Patch.Building"];
        try
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"gupatch_{DateTime.Now:yyyyMMddHHmmss}");
            Directory.CreateDirectory(tmp);
            L(_loc.T("Patch.TempDir", tmp));
            Progress = 20;
            L(_loc["Patch.Comparing"]);
            await _diff.GeneratePatchAsync(Config.OldDirectory, Config.NewDirectory, tmp);
            Progress = 60;
            L(_loc["Patch.PatchDone"]);

            // ── Encryption scan (if enabled) ─────────────────
            if (Config.EnableEncryptionCheck)
            {
                L(_loc["Patch.Scanning"]);
                var scanResult = await _encScanner.ScanDirectoryAsync(tmp);
                Progress = 70;

                if (scanResult.HasSuspiciousFiles)
                {
                    L(_loc.T("Patch.ScanFound", scanResult.SuspiciousFiles.Count));
                    var choice = await ShowEncryptionDialogAsync(scanResult);

                    switch (choice)
                    {
                        case EncryptionDialogChoice.Cancel:
                            L(_loc["Patch.ScanCancelled"]);
                            Status = _loc["Patch.ScanCancelled"];
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
            else
            {
                Progress = 70;
            }

            // ── Package ──────────────────────────────────────
            var outDir = string.IsNullOrWhiteSpace(Config.OutputPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : Config.OutputPath;
            var name = string.IsNullOrWhiteSpace(Config.PackageName)
                ? $"patch_{DateTime.Now:yyyyMMddHHmmss}"
                : Config.PackageName;
            var zip = Path.Combine(outDir, $"{name}.zip");
            L(_loc.T("Patch.Packing", Path.GetFileName(zip)));
            await _pkg.CompressDirectoryAsync(tmp, zip);
            Directory.Delete(tmp, true);
            Progress = 100;
            Status = _loc.T("Patch.Success", Path.GetFileName(zip), new FileInfo(zip).Length / 1024.0);
            L(Status);

            // ── Auto-upload ──────────────────────────────────
            if (AutoUploadEnabled)
            {
                await UploadPatchAsync(zip);
            }
        }
        catch (Exception ex)
        {
            Status = _loc.T("Patch.Failed", ex.Message);
            L(_loc.T("Patch.Error", ex));
        }
        finally
        {
            IsBuilding = false;
        }
    }

    /// <summary>Upload the generated patch to the configured server.</summary>
    private async Task UploadPatchAsync(string zipPath)
    {
        UploadStatus = _loc["Upload.Uploading"];
        UploadProgress = 0;

        try
        {
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
            var progress = new Progress<UploadProgressEventArgs>(e =>
            {
                UploadProgress = e.Percentage;
                UploadStatus = $"{e.Phase}: {e.Percentage:F0}%";
                if (e.Phase == UploadPhase.Completed)
                    L(_loc["Upload.Success"]);
                else if (e.Phase == UploadPhase.Failed)
                    L(_loc.T("Upload.Failed", "Upload error"));
            });

            var result = await uploadSvc.UploadAsync(zipPath, uploadConfig, progress);
            if (result.Success)
            {
                UploadStatus = _loc["Upload.Success"];
                L(UploadStatus);
            }
            else
            {
                UploadStatus = _loc.T("Upload.Failed", result.ErrorMessage ?? "Unknown error");
                L(UploadStatus);
            }
        }
        catch (Exception ex)
        {
            UploadStatus = _loc.T("Upload.Failed", ex.Message);
            L(UploadStatus);
        }
    }

    void L(string m) => Log.Add($"[{DateTime.Now:HH:mm:ss}] {m}");

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

        var rootPanel = new Avalonia.Controls.StackPanel
        {
            Margin = new Avalonia.Thickness(20, 16),
            Spacing = 10
        };

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

        var scrollViewer = new Avalonia.Controls.ScrollViewer
        {
            Content = listPanel,
            MaxHeight = 250,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
        rootPanel.Children.Add(scrollViewer);

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
        var header = new Avalonia.Controls.TextBlock
        {
            Text = $"{title} ({files.Count})",
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            FontSize = 13,
            Margin = new Avalonia.Thickness(0, 4, 0, 2)
        };
        parent.Children.Add(header);
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
