using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.Models;
using GeneralUpdate.Tools.Pipeline;
using GeneralUpdate.Tools.Pipeline.Steps;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.ViewModels;

public partial class ConfigViewModel : ViewModelBase
{
    private readonly LocalizationService _loc = LocalizationService.Instance;

    public ConfigGeneratorModel Model { get; } = new();
    public List<string> AppTypes { get; } = new() { "Client", "Upgrade", "OssClient", "OssUpgrade" };

    public ConfigViewModel()
    {
        Model.PropertyChanged += (_, _) => UpdatePreview();
    }

    private static Avalonia.Controls.TopLevel? GetTopLevel()
    {
        return Avalonia.Controls.TopLevel.GetTopLevel(
            (Avalonia.Application.Current?.ApplicationLifetime as
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow);
    }

    // ════════════════════════════════════════════════════════
    // File pickers
    // ════════════════════════════════════════════════════════

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
            Model.ClientPath = files[0].Path.LocalPath;
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
            Model.UpgradePath = files[0].Path.LocalPath;
    }

    // ════════════════════════════════════════════════════════
    // Analyze
    // ════════════════════════════════════════════════════════

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

    // ════════════════════════════════════════════════════════
    // Generate
    // ════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task Generate()
    {
        // Validate semver
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

        var ctx = new PipelineContext
        {
            ClientPath = Model.ClientPath,
            UpgradePath = Model.UpgradePath,
            Manifest = manifest
        };

        // Run pipeline: parse → validate semver → build → emit
        var orchestrator = new PipelineOrchestrator()
            .AddStep(new CsprojParseStep())
            .AddStep(new SemverValidateStep())
            .AddStep(new ManifestBuildStep())
            .AddStep(new FileEmitStep());

        ctx = await orchestrator.RunAsync(ctx);

        if (ctx.Errors.Count > 0)
        {
            Model.StatusText = string.Join("; ", ctx.Errors);
            return;
        }

        var outputDir = AppContext.BaseDirectory;
        var outputPath = Path.Combine(outputDir, "generalupdate.manifest.json");
        Model.StatusText = $"{_loc["Config.Generated"]}: {outputPath}";

        await DialogHelper.ShowInfoAsync(_loc["Config.Title"], _loc["Config.Generated"] + "\n" + outputPath);

        if (Model.OpenManifestDir)
            OpenFolder(outputDir);
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

    // ════════════════════════════════════════════════════════
    // Generate sample project structure
    // ════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task GenerateSample()
    {
        if (string.IsNullOrWhiteSpace(Model.ClientPath))
        {
            Model.StatusText = _loc["Config.NoClientPath"];
            return;
        }

        Model.IsAnalyzing = true;
        Model.StatusText = _loc["Config.Publishing"];

        try
        {
            var client = CsprojParserService.Parse(Model.ClientPath);
            CsprojInfo? upgrade = null;
            if (!string.IsNullOrWhiteSpace(Model.UpgradePath))
                upgrade = CsprojParserService.Parse(Model.UpgradePath);

            if (client == null)
            {
                Model.StatusText = _loc["Config.Failed"] + ": parse client";
                return;
            }

            var output = await SamplePublisherService.PublishAsync(client, upgrade, Model.UpdatePath);
            Model.StatusText = $"{_loc["Config.SampleGenerated"]}: {output}";

            await DialogHelper.ShowInfoAsync(_loc["Config.Title"],
                _loc["Config.SampleGenerated"] + "\n" + output);

            if (Model.OpenSampleDir)
                OpenFolder(output);
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

    // ════════════════════════════════════════════════════════
    // Preview
    // ════════════════════════════════════════════════════════

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
