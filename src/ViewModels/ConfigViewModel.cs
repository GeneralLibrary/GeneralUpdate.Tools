using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.Configuration;
using GeneralUpdate.Tools.Models;
using GeneralUpdate.Tools.Pipeline;
using GeneralUpdate.Tools.Pipeline.Steps;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.ViewModels;

public partial class ConfigViewModel : ViewModelBase
{
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private readonly AppConfig _config;

    public ConfigGeneratorModel Model { get; } = new();
    public List<string> AppTypes { get; } = new() { "Client", "Upgrade", "OssClient", "OssUpgrade" };

    public bool IsBusy => Model.IsAnalyzing || Model.IsPublishing;

    public ConfigViewModel(AppConfig config)
    {
        _config = config;

        // Restore path memory
        Model.ClientPath = config.LastConfigClientPath;
        Model.UpgradePath = config.LastConfigUpgradePath;

        Model.PropertyChanged += (_, e) =>
        {
            UpdatePreview();
            if (e.PropertyName is nameof(ConfigGeneratorModel.IsAnalyzing) or nameof(ConfigGeneratorModel.IsPublishing))
                OnPropertyChanged(nameof(IsBusy));
        };
    }

    private static Avalonia.Controls.TopLevel? GetTopLevel()
    {
        return Avalonia.Controls.TopLevel.GetTopLevel(
            (Avalonia.Application.Current?.ApplicationLifetime as
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow);
    }

    [RelayCommand]
    private async Task BrowseClient()
    {
        var tl = GetTopLevel();
        if (tl == null) return;
        var filter = new System.Collections.Generic.List<Avalonia.Platform.Storage.FilePickerFileType>
        {
            new("C# Project") { Patterns = new[] { "*.csproj" } }
        };
        var files = await tl.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = _loc["Config.BrowseClient"],
            AllowMultiple = false,
            FileTypeFilter = filter
        });
        if (files.Count > 0)
        {
            Model.ClientPath = files[0].Path.LocalPath;
            _config.LastConfigClientPath = files[0].Path.LocalPath;
            ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
        }
    }

    [RelayCommand]
    private async Task BrowseUpgrade()
    {
        var tl = GetTopLevel();
        if (tl == null) return;
        var filter = new System.Collections.Generic.List<Avalonia.Platform.Storage.FilePickerFileType>
        {
            new("C# Project") { Patterns = new[] { "*.csproj" } }
        };
        var files = await tl.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = _loc["Config.BrowseUpgrade"],
            AllowMultiple = false,
            FileTypeFilter = filter
        });
        if (files.Count > 0)
        {
            Model.UpgradePath = files[0].Path.LocalPath;
            _config.LastConfigUpgradePath = files[0].Path.LocalPath;
            ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
        }
    }

    [RelayCommand]
    private async Task Analyze()
    {
        if (string.IsNullOrWhiteSpace(Model.ClientPath))
        {
            Model.StatusText = _loc["Config.NoClientPath"];
            return;
        }

        Model.IsAnalyzing = true;
        Model.StatusText = _loc["Config.Analyzing"];

        try
        {
            var client = CsprojParserService.Parse(Model.ClientPath);
            CsprojInfo? upgrade = null;
            if (!string.IsNullOrWhiteSpace(Model.UpgradePath))
                upgrade = CsprojParserService.Parse(Model.UpgradePath);

            if (client == null)
            {
                Model.StatusText = _loc["Config.Failed"] + ": unable to parse client .csproj";
                return;
            }

            Model.MainAppName = client.AssemblyName;
            Model.ClientFramework = client.TargetFramework;

            if (upgrade != null)
            {
                Model.UpdateAppName = upgrade.AssemblyName;
                Model.UpgradeFramework = upgrade.TargetFramework;
            }
            else
            {
                Model.UpdateAppName = "Update.exe";
            }

            Model.IsAnalyzed = true;
            Model.StatusText = _loc["Config.Analyzed"];
            UpdatePreview();
        }
        catch (Exception ex)
        {
            Model.StatusText = $"{_loc["Config.Failed"]}: {ex.Message}";
        }
        finally
        {
            Model.IsAnalyzing = false;
        }
    }

    [RelayCommand]
    private async Task Generate()
    {
        var manifest = new ManifestModel
        {
            MainAppName = Model.MainAppName,
            ClientVersion = Model.ClientVersion,
            AppType = Model.AppType,
            UpdateAppName = Model.UpdateAppName,
            UpgradeClientVersion = Model.UpgradeClientVersion,
            ProductId = Model.ProductId,
            UpdatePath = Model.UpdatePath
        };

        var outputDir = AppContext.BaseDirectory;

        await DialogHelper.ShowResultWindowAsync(
            _loc["Config.Title"],
            async progress =>
            {
                var ctx = new PipelineContext
                {
                    ClientPath = Model.ClientPath,
                    UpgradePath = Model.UpgradePath,
                    Manifest = manifest
                };

                var orchestrator = new PipelineOrchestrator()
                    .AddStep(new CsprojParseStep())
                    .AddStep(new SemverValidateStep())
                    .AddStep(new ManifestBuildStep())
                    .AddStep(new FileEmitStep());

                progress.Report($"[{DateTime.Now:HH:mm:ss}] Generating manifest...");
                ctx = await orchestrator.RunAsync(ctx);

                if (ctx.Errors.Count > 0)
                {
                    progress.Report($"[{DateTime.Now:HH:mm:ss}] Errors: {string.Join("; ", ctx.Errors)}");
                    return;
                }

                var outputPath = Path.Combine(outputDir, "generalupdate.manifest.json");
                progress.Report($"[{DateTime.Now:HH:mm:ss}] {_loc["Config.Generated"]}: {outputPath}");

                if (Model.OpenManifestDir)
                    OpenFolder(outputDir);
            },
            outputDir);
    }

    private static void OpenFolder(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch
        {
            // best-effort
        }
    }

    [RelayCommand]
    private async Task GenerateSample()
    {
        if (string.IsNullOrWhiteSpace(Model.ClientPath))
        {
            return;
        }

        Model.IsPublishing = true;
        try
        {
            await DialogHelper.ShowResultWindowAsync(
                _loc["Config.Title"],
                async progress =>
                {
                    progress.Report($"[{DateTime.Now:HH:mm:ss}] Publishing...");

                    var client = CsprojParserService.Parse(Model.ClientPath);
                    CsprojInfo? upgrade = null;
                    if (!string.IsNullOrWhiteSpace(Model.UpgradePath))
                        upgrade = CsprojParserService.Parse(Model.UpgradePath);

                    if (client == null)
                    {
                        progress.Report($"[{DateTime.Now:HH:mm:ss}] Failed: parse client");
                        return;
                    }

                    var manifest = ManifestGeneratorService.FromCsprojInfo(client, upgrade,
                        new ManifestModel
                        {
                            MainAppName = Model.MainAppName,
                            ClientVersion = Model.ClientVersion,
                            AppType = Model.AppType,
                            UpdateAppName = Model.UpdateAppName,
                            UpgradeClientVersion = Model.UpgradeClientVersion,
                            ProductId = Model.ProductId,
                            UpdatePath = Model.UpdatePath
                        });

                    var validateCtx = new PipelineContext();
                    SemverValidateStep.Validate(manifest.ClientVersion, nameof(manifest.ClientVersion), validateCtx);
                    SemverValidateStep.Validate(manifest.UpgradeClientVersion, nameof(manifest.UpgradeClientVersion), validateCtx);
                    if (validateCtx.Errors.Count > 0)
                    {
                        progress.Report($"[{DateTime.Now:HH:mm:ss}] Errors: {string.Join("; ", validateCtx.Errors)}");
                        return;
                    }

                    var output = await SamplePublisherService.PublishAsync(client, upgrade, Model.UpdatePath, manifest: manifest);
                    progress.Report($"[{DateTime.Now:HH:mm:ss}] {_loc["Config.SampleGenerated"]}: {output}");

                    if (Model.OpenSampleDir)
                        OpenFolder(output);
                },
                null);
        }
        finally
        {
            Model.IsPublishing = false;
        }
    }

    private void UpdatePreview()
    {
        var manifest = new ManifestModel
        {
            MainAppName = Model.MainAppName,
            ClientVersion = Model.ClientVersion,
            AppType = Model.AppType,
            UpdateAppName = Model.UpdateAppName,
            UpgradeClientVersion = Model.UpgradeClientVersion,
            ProductId = Model.ProductId,
            UpdatePath = Model.UpdatePath
        };
        Model.PreviewJson = ManifestGeneratorService.GenerateJson(manifest);
    }
}
