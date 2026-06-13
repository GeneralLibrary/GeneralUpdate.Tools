using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.Configuration;
using GeneralUpdate.Tools.Models;
using GeneralUpdate.Tools.Services;
using GeneralUpdate.Tools.Services.Mobile;

namespace GeneralUpdate.Tools.ViewModels;

public partial class MobilePackagingViewModel : ViewModelBase
{
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private readonly AppConfig _config;
    private readonly FormatDetector _formatDetector = new();
    private readonly HashService _hashService = new();

    public MobilePackageModel Model { get; } = new();

    [ObservableProperty] private bool _isProcessing;

    private IProgress<string>? _opProgress;
    private bool _initialized;

    public MobilePackagingViewModel(AppConfig config)
    {
        _config = config;

        Model.FilePath = config.LastMobileFilePath;
        Model.OutputDirectory = config.LastMobileOutputDir;
        Model.ProductId = config.LastMobileProductId;
        Model.Platform = config.LastMobilePlatform;
        Model.AppType = config.LastMobileAppType;
        Model.UseProjectMode = config.LastMobileUseProjectMode;

        Model.PropertyChanged += (_, e) =>
        {
            if (!_initialized) return;
            switch (e.PropertyName)
            {
                case nameof(MobilePackageModel.UseProjectMode):
                    _config.LastMobileUseProjectMode = Model.UseProjectMode;
                    ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
                    ClearModel();
                    break;
            }
        };

        _initialized = true;
    }

    private void ClearModel()
    {
        Model.FilePath = "";
        Model.Format = PackageFormat.Unknown;
        Model.FormatDisplay = "";
        Model.ProjectPath = "";
        Model.ProjectType = "";
        Model.ProjectBuildOutput = "";
        Model.PackageName = "";
        Model.VersionName = "";
        Model.VersionCode = "";
        Model.Sha256Hash = "";
        Model.FileSize = 0;
        Model.FileSizeDisplay = "";
    }

    private static Avalonia.Controls.TopLevel? GetTopLevel()
    {
        return Avalonia.Controls.TopLevel.GetTopLevel(
            (Avalonia.Application.Current?.ApplicationLifetime as
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow);
    }

    // ── Commands ────────────────────────────────────────────

    [RelayCommand]
    private async Task SelectFile()
    {
        var tl = GetTopLevel();
        if (tl == null) return;

        var filter = new System.Collections.Generic.List<Avalonia.Platform.Storage.FilePickerFileType>
        {
            new("Mobile Packages") { Patterns = new[] { "*.apk", "*.aab" } }
        };

        var files = await tl.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = _loc["Mobile.SelectFile"],
            AllowMultiple = false,
            FileTypeFilter = filter
        });

        if (files.Count == 0) return;

        Model.FilePath = files[0].Path.LocalPath;
        _config.LastMobileFilePath = Model.FilePath;
        ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);

        var detectResult = _formatDetector.Detect(Model.FilePath);
        if (detectResult.Success)
        {
            Model.Format = detectResult.Format;
            Model.FormatDisplay = detectResult.DisplayName ?? "";
        }
        else
        {
            Model.Format = PackageFormat.Unknown;
            Model.FormatDisplay = "?";
        }
    }

    [RelayCommand]
    private async Task SelectProject()
    {
        var tl = GetTopLevel();
        if (tl == null) return;

        var filter = new System.Collections.Generic.List<Avalonia.Platform.Storage.FilePickerFileType>
        {
            new("C# Project") { Patterns = new[] { "*.csproj" } }
        };

        var files = await tl.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = _loc["Mobile.SelectProject"],
            AllowMultiple = false,
            FileTypeFilter = filter
        });

        if (files.Count == 0) return;

        Model.ProjectPath = files[0].Path.LocalPath;
        _config.LastMobileProjectPath = Model.ProjectPath;
        ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);

        var csprojInfo = MobileCsprojParser.Parse(Model.ProjectPath);
        if (csprojInfo == null || !csprojInfo.Success)
            return;

        Model.ProjectType = MobileCsprojParser.GetProjectTypeDisplay(csprojInfo);
        Model.PackageName = csprojInfo.PackageName ?? "";
        Model.VersionName = csprojInfo.VersionName ?? "";
        Model.VersionCode = csprojInfo.VersionCode ?? "";
    }

    [RelayCommand]
    private async Task SelectOutput()
    {
        var tl = GetTopLevel();
        if (tl == null) return;

        var folders = await tl.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = _loc["Mobile.SelectOutput"],
                AllowMultiple = false
            });

        if (folders.Count == 0) return;

        Model.OutputDirectory = folders[0].Path.LocalPath;
        _config.LastMobileOutputDir = Model.OutputDirectory;
        ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
    }

    [RelayCommand]
    private async Task Analyze()
    {
        if (Model.UseProjectMode)
        {
            if (string.IsNullOrWhiteSpace(Model.ProjectPath))
                return;
            await SelectProject();
            return;
        }

        if (string.IsNullOrWhiteSpace(Model.FilePath) || !File.Exists(Model.FilePath))
            return;

        IsProcessing = true;
        try
        {
            await DialogHelper.ShowResultWindowAsync(
                _loc["Mobile.ResultTitle"],
                async progress =>
                {
                    _opProgress = progress;
                    L("Analyzing package...");

                    if (Model.Format == PackageFormat.Unknown)
                    {
                        var detectResult = _formatDetector.Detect(Model.FilePath);
                        if (detectResult.Success)
                        {
                            Model.Format = detectResult.Format;
                            Model.FormatDisplay = detectResult.DisplayName ?? "";
                        }
                    }

                    var manifestEntry = Model.Format switch
                    {
                        PackageFormat.Aab => "base/manifest/AndroidManifest.xml",
                        _ => "AndroidManifest.xml"
                    };

                    var parser = new AxmlParser();
                    var result = parser.ParseFromZip(Model.FilePath, manifestEntry);

                    if (result.Success)
                    {
                        Model.PackageName = result.PackageName ?? Model.PackageName;
                        Model.VersionName = result.VersionName ?? Model.VersionName;
                        Model.VersionCode = result.VersionCode ?? Model.VersionCode;
                        L($"Package: {Model.PackageName}, Version: {Model.VersionName} (Code: {Model.VersionCode})");
                    }
                    else
                    {
                        L($"Metadata extraction warning: {result.ErrorMessage}. You can fill in manually.");
                    }

                    L("Computing SHA256...");
                    Model.Sha256Hash = await _hashService.ComputeHashAsync(Model.FilePath);
                    L($"SHA256: {Model.Sha256Hash}");

                    var fi = new FileInfo(Model.FilePath);
                    Model.FileSize = fi.Length;
                    Model.FileSizeDisplay = FormatFileSize(fi.Length);
                    L($"File Size: {Model.FileSizeDisplay}");

                    L("Analysis complete.");
                },
                null);
        }
        finally
        {
            _opProgress = null;
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task BuildAndLocate()
    {
        if (!Model.UseProjectMode) return;
        if (string.IsNullOrWhiteSpace(Model.ProjectPath) || !File.Exists(Model.ProjectPath))
            return;

        var csprojInfo = MobileCsprojParser.Parse(Model.ProjectPath);
        if (csprojInfo == null || !csprojInfo.Success)
            return;

        var publishDir = MobileCsprojParser.GetDefaultPublishDir(csprojInfo);

        IsProcessing = true;
        try
        {
            await DialogHelper.ShowResultWindowAsync(
                _loc["Mobile.ResultTitle"],
                async progress =>
                {
                    _opProgress = progress;
                    L($"Building {csprojInfo.ProjectName}...");
                    await RunDotnetPublishAsync(Model.ProjectPath, publishDir);
                    L($"dotnet publish completed: {publishDir}");

                    var ext = MobileCsprojParser.GetExpectedExtension(csprojInfo);
                    var files = Directory.GetFiles(publishDir, $"*{ext}");
                    if (files.Length == 0)
                    {
                        var allApk = Directory.GetFiles(publishDir, "*.apk");
                        var allAab = Directory.GetFiles(publishDir, "*.aab");
                        if (allApk.Length > 0) files = allApk;
                        else if (allAab.Length > 0) files = allAab;
                    }

                    if (files.Length == 0)
                    {
                        L($"Build output not found in: {publishDir}");
                        return;
                    }

                    Model.ProjectBuildOutput = files[0];
                    Model.FilePath = files[0];
                    L($"Build output: {Path.GetFileName(Model.FilePath)}");

                    var detectResult = _formatDetector.Detect(Model.FilePath);
                    if (detectResult.Success)
                    {
                        Model.Format = detectResult.Format;
                        Model.FormatDisplay = detectResult.DisplayName ?? "";
                    }

                    Model.Sha256Hash = await _hashService.ComputeHashAsync(Model.FilePath);
                    L($"SHA256: {Model.Sha256Hash}");

                    var fi = new FileInfo(Model.FilePath);
                    Model.FileSize = fi.Length;
                    Model.FileSizeDisplay = FormatFileSize(fi.Length);
                    L($"File Size: {Model.FileSizeDisplay}");

                    L("Build & locate complete.");
                },
                publishDir);
        }
        finally
        {
            _opProgress = null;
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task Upload()
    {
        if (!Model.UseProjectMode && (string.IsNullOrWhiteSpace(Model.FilePath) || !File.Exists(Model.FilePath)))
            return;

        if (string.IsNullOrWhiteSpace(Model.ProductId)) return;

        if (string.IsNullOrWhiteSpace(Model.Sha256Hash))
            Model.Sha256Hash = await _hashService.ComputeHashAsync(Model.FilePath);

        var outDir = string.IsNullOrWhiteSpace(Model.OutputDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : Model.OutputDirectory;

        IsProcessing = true;
        try
        {
            await DialogHelper.ShowResultWindowAsync(
                _loc["Mobile.ResultTitle"],
                async progress =>
                {
                    _opProgress = progress;
                    var format = Model.Format switch
                    {
                        PackageFormat.Aab => ".aab",
                        _ => ".apk"
                    };

                    var formFields = new System.Collections.Generic.Dictionary<string, string>
                    {
                        ["Name"] = Model.ProductName,
                        ["Version"] = Model.VersionName,
                        ["Hash"] = Model.Sha256Hash,
                        ["Format"] = format,
                        ["Size"] = Model.FileSize.ToString(),
                        ["AppType"] = "1",
                        ["Platform"] = Model.Platform.ToString(),
                        ["ProductId"] = Model.ProductId,
                        ["IsForcibly"] = Model.IsForcibly ? "true" : "false"
                    };

                    var uploadConfig = new UploadConfig
                    {
                        ServerUrl = _config.UploadServerUrl,
                        UploadEndpoint = string.IsNullOrWhiteSpace(_config.UploadEndpoint)
                            ? "/Packet/Create"
                            : _config.UploadEndpoint,
                        TimeoutSeconds = _config.UploadTimeoutSeconds,
                        RetryCount = _config.UploadRetryCount,
                        Auth = _config.UploadAuth,
                        AutoUploadEnabled = true
                    };

                    L("Uploading...");
                    var uploadSvc = new HttpUploadService();
                    var result = await uploadSvc.UploadAsync(Model.FilePath, uploadConfig, formFields);

                    if (result.Success)
                    {
                        L("Upload successful.");
                        _config.LastMobileProductId = Model.ProductId;
                        _config.LastMobilePlatform = Model.Platform;
                        _config.LastMobileAppType = Model.AppType;
                        ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);

                        await ExportRecord(result.UploadUrl);
                    }
                    else
                    {
                        L($"Upload failed: {result.ErrorMessage}");
                    }
                },
                outDir);
        }
        finally
        {
            _opProgress = null;
            IsProcessing = false;
        }
    }

    private async Task ExportRecord(string uploadUrl)
    {
        var format = Model.Format switch
        {
            PackageFormat.Aab => ".aab",
            _ => ".apk"
        };

        var record = new MobileVersionRecord
        {
            Name = Model.ProductName,
            Version = Model.VersionName,
            Hash = Model.Sha256Hash,
            Url = uploadUrl,
            PackageName = Model.PackageName,
            FileSize = Model.FileSize,
            Format = format.TrimStart('.'),
            Platform = Model.Platform,
            AppType = Model.AppType,
            ProductId = Model.ProductId,
            IsForcibly = Model.IsForcibly,
            ReleaseDate = DateTime.UtcNow.ToString("o")
        };

        var outDir = string.IsNullOrWhiteSpace(Model.OutputDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : Model.OutputDirectory;

        if (!Directory.Exists(outDir))
            Directory.CreateDirectory(outDir);

        var fileName = $"mobile_version_{DateTime.Now:yyyyMMddHHmmss}.json";
        var outPath = Path.Combine(outDir, fileName);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(record,
            Newtonsoft.Json.Formatting.Indented,
            new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });

        await File.WriteAllTextAsync(outPath, json);
        L($"Version record saved: {outPath}");
    }

    [RelayCommand]
    private async Task ExportRecordOnly()
    {
        if (string.IsNullOrWhiteSpace(Model.FilePath) || !File.Exists(Model.FilePath))
            return;

        if (string.IsNullOrWhiteSpace(Model.Sha256Hash))
            Model.Sha256Hash = await _hashService.ComputeHashAsync(Model.FilePath);

        var outDir = string.IsNullOrWhiteSpace(Model.OutputDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : Model.OutputDirectory;

        await DialogHelper.ShowResultWindowAsync(
            _loc["Mobile.ResultTitle"],
            async progress =>
            {
                _opProgress = progress;
                await ExportRecord("manual");
            },
            outDir);
    }

    [RelayCommand]
    private void OpenOutputDir()
    {
        var dir = string.IsNullOrWhiteSpace(Model.OutputDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : Model.OutputDirectory;

        if (!Directory.Exists(dir)) return;

        try
        {
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
        catch { }
    }

    // ── dotnet publish ──────────────────────────────────────

    private static async Task RunDotnetPublishAsync(string csprojPath, string outputDir)
    {
        outputDir = outputDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var psi = new ProcessStartInfo("dotnet",
            $"publish \"{csprojPath}\" -c Release -o \"{outputDir}\" --nologo")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi)!;
        var outTask = p.StandardOutput.ReadToEndAsync();
        var errTask = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        var stdout = await outTask;
        var stderr = await errTask;

        if (p.ExitCode != 0)
        {
            var msg = (stderr?.Trim()?.Length > 0 ? stderr + "\n" : "")
                    + (stdout?.Trim()?.Length > 0 ? stdout : "");
            throw new InvalidOperationException($"dotnet publish failed (exit {p.ExitCode}):\n{msg}");
        }
    }

    // ── Helpers ─────────────────────────────────────────────

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }

    private void L(string m) => _opProgress?.Report($"[{DateTime.Now:HH:mm:ss}] {m}");
}
