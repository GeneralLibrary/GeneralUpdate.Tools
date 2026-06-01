using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeneralUpdate.Tools.Models;

namespace GeneralUpdate.Tools.Services;

public class SimulationService
{
    private readonly LocalUpdateServer _server = new();
    private readonly StringBuilder _fullLog = new();
    private int _timeoutSeconds = 120;

    public async Task<SimulationResult> RunAsync(
        SimulateConfigModel config,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var result = new SimulationResult();
        var sw = Stopwatch.StartNew();

        try
        {
            // 1. Validate
            Log("STEP 1: Validating inputs", progress);
            Validate(config);

            // 2. Prepare app directory
            Log($"STEP 2: Preparing {config.AppDirectory}", progress);
            Directory.CreateDirectory(config.AppDirectory);

            // 3. Publish test apps — same pattern as Config Generator's "Generate Sample"
            Log("STEP 3: Publishing test apps", progress);
            var toolsDir = AppDomain.CurrentDomain.BaseDirectory;
            var updatePath = (config.UpdatePath ?? "update/").Trim('/').Trim('\\');
            var upgradeSubDir = Path.Combine(config.AppDirectory, updatePath);
            Directory.CreateDirectory(upgradeSubDir);

            var clientExeName = "ClientSample.exe";
            var upgradeExeName = "UpgradeSample.exe";

            var clientProj = Path.GetFullPath(Path.Combine(toolsDir, "..", "..", "..", "..", "test_app", "Client", "ClientSample.csproj"));
            var upgradeProj = Path.GetFullPath(Path.Combine(toolsDir, "..", "..", "..", "..", "test_app", "Upgrade", "UpgradeSample.csproj"));

            if (!File.Exists(clientProj) || !File.Exists(upgradeProj))
                throw new FileNotFoundException("Test apps not found.");

            // Always publish fresh — matches Config Generator's SamplePublisherService pattern
            Log($"  Publishing ClientSample → {config.AppDirectory}", progress);
            await DotNetPublishAsync(clientProj, config.AppDirectory);
            Log($"  Publishing UpgradeSample → {upgradeSubDir}", progress);
            await DotNetPublishAsync(upgradeProj, upgradeSubDir);
            Log("  Test apps published successfully", progress);

            // Verify the executables were actually produced
            var clientExePath = Path.Combine(config.AppDirectory, clientExeName);
            var upgradeExePath = Path.Combine(upgradeSubDir, upgradeExeName);
            if (!File.Exists(clientExePath))
                throw new FileNotFoundException($"Publish did not produce {clientExePath}");
            if (!File.Exists(upgradeExePath))
                throw new FileNotFoundException($"Publish did not produce {upgradeExePath}");

            // 3.5 Generate generalupdate.manifest.json — names match published output exactly
            Log("STEP 3.5: Generating manifest", progress);
            var manifest = new ManifestModel
            {
                MainAppName = clientExeName,
                ClientVersion = config.CurrentVersion,
                AppType = config.AppType switch { 1 => "Client", 2 => "Upgrade", _ => "Client" },
                UpdateAppName = upgradeExeName,
                UpgradeClientVersion = "1.0.0",
                ProductId = config.ProductId,
                UpdatePath = config.UpdatePath ?? "update/"
            };
            var manifestJson = ManifestGeneratorService.GenerateJson(manifest);
            var manifestPath = Path.Combine(config.AppDirectory, "generalupdate.manifest.json");
            await File.WriteAllTextAsync(manifestPath, manifestJson);
            Log($"  Manifest → {manifestPath}", progress);

            // 4. Start server
            Log("STEP 4: Starting local server", progress);
            var serverPatchDir = Path.Combine(config.AppDirectory, ".server");
            Directory.CreateDirectory(serverPatchDir);
            var patchName = Path.GetFileName(config.PatchFilePath);
            var patchDest = Path.Combine(serverPatchDir, patchName);
            File.Copy(config.PatchFilePath, patchDest, true);

            var hash = ComputeQuickHash(patchDest);
            LocalUpdateServerFiles.Register(patchName, patchDest);
            _server.Versions.Add(new VersionRecord
            {
                CurrentVersion = config.CurrentVersion,
                TargetVersion = config.TargetVersion,
                Hash = hash,
                ZipPath = patchDest,
                AppType = config.AppType,
                Platform = config.Platform,
                ProductId = config.ProductId
            });

            await _server.StartAsync(config.ServerPort);
            Log($"  Server running on {_server.BaseUrl}", progress);
            config.ServerPort = _server.Port;

            // 5. Run client
            Log("STEP 5: Running Client", progress);
            var clientExe = Path.Combine(config.AppDirectory, "ClientSample.exe");
            var clientArgs = new List<string>
            {
                "--server-url", _server.BaseUrl,
                "--app-secret", config.AppSecretKey,
                "--client-version", config.CurrentVersion
            };
            var clientResult = await RunExe(clientExe, clientArgs, ct);
            Log(clientResult.Output, progress);

            // 5.5 Stop server IMMEDIATELY — prevents the new ClientSample.exe
            // instance launched by Upgrade from reconnecting and looping.
            Log("STEP 5.5: Stopping server", progress);
            try { await _server.DisposeAsync(); } catch (Exception ex) { Log($"  Server shutdown warning: {ex.Message}", progress); }
            LocalUpdateServerFiles.Clear();

            if (!clientResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = "Client exited with error";
                return result;
            }

            Log("  Client completed successfully", progress);

            // 6. Verify
            Log("STEP 6: Verifying update result", progress);
            await Task.Delay(2000, ct);
            VerifyUpdateResult(config, result);

            result.Success = true;
            result.Elapsed = sw.Elapsed;
            Log($"✅ Simulation complete ({sw.Elapsed.TotalSeconds:F1}s)", progress);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            Log($"❌ Simulation failed: {ex.Message}", progress);
        }
        finally
        {
            try { await _server.DisposeAsync(); } catch (Exception ex) { _fullLog.AppendLine($"Server shutdown warning: {ex.Message}"); }
            LocalUpdateServerFiles.Clear();
            result.FullLog = _fullLog.ToString();
        }

        return result;
    }

    private static async Task DotNetPublishAsync(string projectPath, string outputDir)
    {
        // Trim trailing backslash — otherwise the \" in the command line gets escaped
        outputDir = outputDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        // Same pattern as SamplePublisherService.RunDotnetPublishAsync
        var psi = new ProcessStartInfo("dotnet", $"publish \"{projectPath}\" -c Release -o \"{outputDir}\" --nologo")
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
            var message = (stderr?.Trim()?.Length > 0 ? stderr + "\n" : "")
                        + (stdout?.Trim()?.Length > 0 ? stdout : "");
            throw new InvalidOperationException($"dotnet publish failed (exit {p.ExitCode}):\n{message}");
        }
    }

    private void Validate(SimulateConfigModel config)
    {
        if (!Directory.Exists(config.AppDirectory))
            throw new DirectoryNotFoundException($"App directory not found: {config.AppDirectory}");
        if (!File.Exists(config.PatchFilePath))
            throw new FileNotFoundException($"Patch file not found: {config.PatchFilePath}");
        try
        {
            var psi = new ProcessStartInfo("dotnet", "--version") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi); p?.WaitForExit(5000);
            var ver = p?.StandardOutput.ReadToEnd().Trim();
            if (string.IsNullOrEmpty(ver) || !ver.StartsWith("10.") && !ver.StartsWith("11."))
                throw new InvalidOperationException(".NET 10.0 SDK required");
        }
        catch (InvalidOperationException) { throw; }
        catch { throw new InvalidOperationException("dotnet CLI not found"); }
    }

    private async Task<(bool Success, string Output)> RunExe(string exePath, List<string> arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exePath)
        {
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false, CreateNoWindow = true
        };
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);
        using var p = Process.Start(psi)!;
        var output = new StringBuilder();
        var readTask = Task.Run(async () => { while (!p.StandardOutput.EndOfStream) output.AppendLine(await p.StandardOutput.ReadLineAsync(ct)); }, ct);
        var errorTask = Task.Run(async () => { while (!p.StandardError.EndOfStream) output.AppendLine(await p.StandardError.ReadLineAsync(ct)); }, ct);
        var completed = p.WaitForExit(_timeoutSeconds * 1000);
        if (!completed) { p.Kill(true); return (false, output + "\n[TIMEOUT]"); }
        await Task.WhenAll(readTask, errorTask);
        var outputStr = output.ToString();
        var hasError = outputStr.Contains("ERROR:") || outputStr.Contains("FATAL:") || outputStr.Contains("[Error]") || outputStr.Contains("JsonException");
        return (!hasError &&  (p.ExitCode == 0 || p.ExitCode == -1), outputStr);
    }

    private void VerifyUpdateResult(SimulateConfigModel config, SimulationResult result)
    {
        var deleteFile = Path.Combine(config.AppDirectory, "delete_files.json");
        if (File.Exists(deleteFile))
            result.Notes.Add("delete_files.json still present - HandleDeleteList may not have run");

        var fileCount = Directory.GetFiles(config.AppDirectory, "*", SearchOption.AllDirectories).Length;
        result.Notes.Add($"Files in app directory after update: {fileCount}");
    }

    private static string ComputeQuickHash(string filePath)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var fs = File.OpenRead(filePath);
        return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
    }

    private void Log(string msg, IProgress<string>? progress)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        _fullLog.AppendLine(line);
        progress?.Report(line);
    }
}

public class SimulationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Elapsed { get; set; }
    public string FullLog { get; set; } = "";
    public List<string> Notes { get; } = new();
}
