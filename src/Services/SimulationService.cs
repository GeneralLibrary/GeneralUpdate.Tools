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
    private readonly ClientGeneratorService _generator = new();
    private readonly LocalUpdateServer _server = new();
    private int _timeoutSeconds = 120;

    public string ServerBaseUrl => _server.BaseUrl;

    /// <summary>
    /// Generate scripts and start the local update server. Server stays running until StopServerAsync is called.
    /// </summary>
    public async Task StartServerAsync(SimulateConfigModel config, IProgress<string>? progress = null)
    {
        Log("STEP 1: Validating inputs", progress);
        Validate(config);

        Log("STEP 2: Generating client.csx and upgrade.csx", progress);
        await _generator.GenerateAsync(config, config.OutputDirectory);
        Log($"  client.csx → {config.OutputDirectory}", progress);
        Log($"  upgrade.csx → {config.OutputDirectory}", progress);

        Log("STEP 3: Starting local server", progress);
        var serverPatchDir = Path.Combine(config.OutputDirectory, ".server");
        Directory.CreateDirectory(serverPatchDir);
        var patchName = Path.GetFileName(config.PatchFilePath);
        var patchDest = Path.Combine(serverPatchDir, patchName);
        File.Copy(config.PatchFilePath, patchDest, true);

        var hash = ComputeQuickHash(patchDest);
        LocalUpdateServerFiles.Register(patchName, patchDest);
        _server.Updates.Add((config.CurrentVersion, config.TargetVersion, hash, patchDest, config.AppType));

        await _server.StartAsync(config.ServerPort);
        config.ServerPort = _server.Port;
        config.ServerRunning = true;
        Log($"  Server running on {_server.BaseUrl}", progress);
    }

    /// <summary>
    /// Stop the local update server.
    /// </summary>
    public async Task StopServerAsync()
    {
        await _server.DisposeAsync();
        LocalUpdateServerFiles.Clear();
    }

    /// <summary>
    /// Run the client script and return results. Server must already be running.
    /// </summary>
    public async Task<SimulationResult> RunClientAsync(SimulateConfigModel config, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var result = new SimulationResult();
        var sw = Stopwatch.StartNew();

        try
        {
            Log("Running client (dotnet script client.csx)", progress);
            var clientResult = await RunDotNetScript(config.OutputDirectory, "client.csx", ct);
            Log(clientResult.Output, progress);

            if (!clientResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = "Client exited with error";
                return result;
            }

            Log("Client completed", progress);
            await Task.Delay(2000, ct);

            var fileCount = Directory.GetFiles(config.AppDirectory, "*", SearchOption.AllDirectories).Length;
            result.Notes.Add($"Files in app directory after update: {fileCount}");

            result.Success = true;
            result.Elapsed = sw.Elapsed;
            Log($"Simulation complete ({sw.Elapsed.TotalSeconds:F1}s)", progress);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            Log($"Simulation failed: {ex.Message}", progress);
        }

        result.FullLog = "";
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

    private async Task<(bool Success, string Output)> RunDotNetScript(string workDir, string script, CancellationToken ct)
    {
        var psi = new ProcessStartInfo("dotnet", $"script {script}")
        {
            WorkingDirectory = workDir,
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

    private static string ComputeQuickHash(string filePath)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var fs = File.OpenRead(filePath);
        return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
    }

    private static void Log(string msg, IProgress<string>? progress) => progress?.Report($"[{DateTime.Now:HH:mm:ss}] {msg}");
}

public class SimulationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Elapsed { get; set; }
    public string FullLog { get; set; } = "";
    public List<string> Notes { get; } = new();
}
