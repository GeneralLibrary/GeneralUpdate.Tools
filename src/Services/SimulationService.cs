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

            // 3. Ensure test apps are available
            Log("STEP 3: Preparing test apps", progress);
            var toolsDir = AppDomain.CurrentDomain.BaseDirectory;
            var clientDest = Path.Combine(config.AppDirectory, "Client.exe");
            var upgradeDest = Path.Combine(config.AppDirectory, "Upgrade.exe");

            if (File.Exists(clientDest) && File.Exists(upgradeDest))
            {
                Log("  Test apps already cached, skipping", progress);
            }
            else
            {
                // Strategy 1: bundled exes (release package)
                // Use ProcessPath not BaseDirectory — single-file publish extracts to a temp dir
                var appRoot = Path.GetDirectoryName(Environment.ProcessPath)!;
                var bundledDir = Path.Combine(appRoot, "test_app_exe");
                if (Directory.Exists(bundledDir) &&
                    File.Exists(Path.Combine(bundledDir, "ClientSample.exe")) &&
                    File.Exists(Path.Combine(bundledDir, "UpgradeSample.exe")))
                {
                    Log("  Copying bundled test apps...", progress);
                    File.Copy(Path.Combine(bundledDir, "ClientSample.exe"), clientDest, true);
                    File.Copy(Path.Combine(bundledDir, "UpgradeSample.exe"), upgradeDest, true);
                    Log($"  Client.exe → {config.AppDirectory}", progress);
                    Log($"  Upgrade.exe → {config.AppDirectory}", progress);
                }
                else
                {
                    // Strategy 2: compile from source (dev environment)
                    var clientProj = Path.GetFullPath(Path.Combine(toolsDir, "..", "..", "..", "..", "test_app", "Client", "ClientSample.csproj"));
                    var upgradeProj = Path.GetFullPath(Path.Combine(toolsDir, "..", "..", "..", "..", "test_app", "Upgrade", "UpgradeSample.csproj"));

                    if (!File.Exists(clientProj) || !File.Exists(upgradeProj))
                        throw new FileNotFoundException("Test apps not found. Run in dev environment or use a release build that includes test_app_exe.");

                    var exeDir = Path.Combine(toolsDir, "test_app");
                    Directory.CreateDirectory(exeDir);

                    Log("  Compiling Client.exe...", progress);
                    await DotNetPublishAsync(clientProj, exeDir);
                    Log($"  Client.exe → {exeDir}", progress);

                    Log("  Compiling Upgrade.exe...", progress);
                    await DotNetPublishAsync(upgradeProj, exeDir);
                    Log($"  Upgrade.exe → {exeDir}", progress);

                    File.Copy(Path.Combine(exeDir, "ClientSample.exe"), clientDest, true);
                    File.Copy(Path.Combine(exeDir, "UpgradeSample.exe"), upgradeDest, true);
                    Log($"  Copied to {config.AppDirectory}", progress);
                }
            }

            // 4. Start server
            Log("STEP 4: Starting local server", progress);
            var serverPatchDir = Path.Combine(config.AppDirectory, ".server");
            Directory.CreateDirectory(serverPatchDir);
            var patchName = Path.GetFileName(config.PatchFilePath);
            var patchDest = Path.Combine(serverPatchDir, patchName);
            File.Copy(config.PatchFilePath, patchDest, true);

            var hash = ComputeQuickHash(patchDest);
            LocalUpdateServerFiles.Register(patchName, patchDest);
            _server.Updates.Add((config.CurrentVersion, config.TargetVersion, hash, patchDest, config.AppType));

            await _server.StartAsync(config.ServerPort);
            Log($"  Server running on {_server.BaseUrl}", progress);
            config.ServerPort = _server.Port;

            // 5. Run client
            Log("STEP 5: Running Client.exe", progress);
            var clientExe = Path.Combine(config.AppDirectory, "Client.exe");
            var clientArgs = new List<string>
            {
                "--server-url", _server.BaseUrl,
                "--install-path", config.AppDirectory,
                "--current-version", config.CurrentVersion,
                "--app-secret", config.AppSecretKey,
                "--product-id", config.ProductId,
                "--app-name", "Upgrade.exe"
            };
            var clientResult = await RunExe(clientExe, clientArgs, ct);
            Log(clientResult.Output, progress);

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
            try { await _server.DisposeAsync(); } catch { }
            LocalUpdateServerFiles.Clear();
            result.FullLog = _fullLog.ToString();
        }

        return result;
    }

    private static async Task DotNetPublishAsync(string projectPath, string outputDir)
    {
        var psi = new ProcessStartInfo("dotnet", $"publish \"{projectPath}\" -c Release -r win-x64 -p:PublishSingleFile=true --self-contained -p:PublishTrimmed=true -o \"{outputDir}\"")
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
        if (p.ExitCode != 0)
        {
            var err = await errTask;
            throw new InvalidOperationException($"dotnet publish failed (exit {p.ExitCode}):\n{err}");
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
        var hasError = outputStr.Contains("ERROR:") || outputStr.Contains("FATAL:") || outputStr.Contains("JsonException");
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
