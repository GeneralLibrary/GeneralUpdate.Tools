using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GeneralUpdate.Tools.Models;

namespace GeneralUpdate.Tools.Services;

public static class SamplePublisherService
{
    /// <summary>
    ///     Runs <c>dotnet publish</c> for the client and upgrade projects,
    ///     then assembles the output into the Tools running directory.
    ///     When <paramref name="manifest"/> is provided, also writes
    ///     <c>generalupdate.manifest.json</c> into the output root.
    /// </summary>
    public static async Task<string> PublishAsync(
        CsprojInfo client,
        CsprojInfo? upgrade,
        string updatePath,
        string? baseOutput = null,
        ManifestModel? manifest = null)
    {
        var outputRoot = baseOutput ?? Path.Combine(AppContext.BaseDirectory, "sample_output");
        if (Directory.Exists(outputRoot))
        {
            try { Directory.Delete(outputRoot, true); }
            catch { /* stale output — proceed with a fresh subfolder */ }
        }
        Directory.CreateDirectory(outputRoot);

        // Publish client
        await RunDotnetPublishAsync(client.CsprojPath, outputRoot);

        // Publish upgrade into the UpdatePath subdirectory
        if (upgrade != null)
        {
            var updateDir = Path.Combine(outputRoot, updatePath.Trim('/').Trim('\\'));
            Directory.CreateDirectory(updateDir);
            await RunDotnetPublishAsync(upgrade.CsprojPath, updateDir);
        }

        // Write manifest.json into the output root if provided
        if (manifest != null)
        {
            var json = ManifestGeneratorService.GenerateJson(manifest);
            var manifestPath = Path.Combine(outputRoot, "generalupdate.manifest.json");
            await File.WriteAllTextAsync(manifestPath, json);
        }

        return outputRoot;
    }

    private static async Task RunDotnetPublishAsync(string csprojPath, string outputDir)
    {
        var psi = new ProcessStartInfo("dotnet", $"publish \"{csprojPath}\" -c Release -o \"{outputDir}\" --nologo")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        // Read stdout/stderr concurrently to prevent pipe-buffer deadlock
        var readOut = process.StandardOutput.ReadToEndAsync();
        var readErr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var err = await readErr;
            throw new InvalidOperationException($"dotnet publish failed for {csprojPath}:\n{err}");
        }
    }
}
