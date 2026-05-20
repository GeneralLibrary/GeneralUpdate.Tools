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

            // 2. Prepare output
            Log($"STEP 2: Preparing {config.OutputDirectory}", progress);
            Directory.CreateDirectory(config.OutputDirectory);

            // 3. Copy test app executables
            Log("STEP 3: Copying test executables", progress);
            var toolsDir = AppDomain.CurrentDomain.BaseDirectory;
            var clientSrc = Path.Combine(toolsDir, "test_app", "Client.exe");
            var upgradeSrc = Path.Combine(toolsDir, "test_app", "Upgrade.exe");

            if (!File.Exists(clientSrc))
                throw new FileNotFoundException($"Client.exe not found at {clientSrc}. Ensure test_app is deployed.");
            if (!File.Exists(upgradeSrc))
                throw new FileNotFoundException($"Upgrade.exe not found at {upgradeSrc}. Ensure test_app is deployed.");

            var clientDest = Path.Combine(config.OutputDirectory, "Client.exe");
            var upgradeDest = Path.Combine(config.OutputDirectory, "Upgrade.exe");
            File.Copy(clientSrc, clientDest, true);
            File.Copy(upgradeSrc, upgradeDest, true);
            Log($"  Client.exe → {config.OutputDirectory}", progress);
            Log($"  Upgrade.exe → {config.OutputDirectory}", progress);

            // 4. Start server
            Log("STEP 4: Starting local server", progress);
            var serverPatchDir = Path.Combine(config.OutputDirectory, ".server");
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
            var clientExe = Path.Combine(config.OutputDirectory, "Client.exe");
            var clientArgs = $"--server-url {_server.BaseUrl} --install-path \"{config.AppDirectory}\" --current-version {config.CurrentVersion} --app-secret {config.AppSecretKey} --product-id {config.ProductId} --app-name Upgrade.exe";
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

    private void Validate(SimulateConfigModel config)
    {
        if (!Directory.Exists(config.AppDirectory))
            throw new DirectoryNotFoundException($"App directory not found: {config.AppDirectory}");
        if (!File.Exists(config.PatchFilePath))
            throw new FileNotFoundException($"Patch file not found: {config.PatchFilePath}");
        if (string.IsNullOrWhiteSpace(config.OutputDirectory))
            throw new ArgumentException("Output directory is required");
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

    private async Task<(bool Success, string Output)> RunExe(string exePath, string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exePath, arguments)
        {
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false, CreateNoWindow = true
        };
        using var p = Process.Start(psi)!;
        var output = new StringBuilder();
        var readTask = Task.Run(async () => { while (!p.StandardOutput.EndOfStream) output.AppendLine(await p.StandardOutput.ReadLineAsync(ct)); }, ct);
        var errorTask = Task.Run(async () => { while (!p.StandardError.EndOfStream) output.AppendLine(await p.StandardError.ReadLineAsync(ct)); }, ct);
        var completed = p.WaitForExit(_timeoutSeconds * 1000);
        if (!completed) { p.Kill(true); return (false, output + "\n[TIMEOUT]"); }
        await Task.WhenAll(readTask, errorTask);
        var outputStr = output.ToString();
        var hasError = outputStr.Contains("ERROR:") || outputStr.Contains("FATAL:") || outputStr.Contains("JsonException");
        return (!hasError && p.ExitCode == 0, outputStr);
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
