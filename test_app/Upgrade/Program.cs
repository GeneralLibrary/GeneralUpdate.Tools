using System.Text.Json;
using GeneralUpdate.Core;

// Parse command-line args
var cliArgs = ParseArgs(Environment.GetCommandLineArgs()[1..]);

// Write ProcessInfo as env var for GeneralUpdateBootstrap
var processInfoJson = JsonSerializer.Serialize(new Dictionary<string, object>
{
    ["AppName"] = "Client.exe",
    ["InstallPath"] = cliArgs.InstallPath,
    ["CurrentVersion"] = cliArgs.CurrentVersion,
    ["LastVersion"] = cliArgs.LastVersion,
    ["CompressEncoding"] = "utf-8",
    ["CompressFormat"] = ".zip",
    ["DownloadTimeOut"] = 60,
    ["AppSecretKey"] = cliArgs.AppSecret,
    ["UpdateVersions"] = new[]
    {
        new Dictionary<string, object?>
        {
            ["Name"] = cliArgs.PatchName,
            ["Version"] = cliArgs.TargetVersion,
            ["Hash"] = cliArgs.Hash,
            ["AppType"] = 1,
            ["IsForcibly"] = false
        }
    }
});

Environment.SetEnvironmentVariable("ProcessInfo", processInfoJson);

Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Upgrade started");
Console.WriteLine($"Install path: {cliArgs.InstallPath}");

try
{
    await new GeneralUpdateBootstrap()
        .AddListenerMultiDownloadStatistics((_, e) =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Download: {e.ProgressPercentage}%");
        })
        .AddListenerMultiAllDownloadCompleted((_, e) =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Downloads: {(e.IsAllDownloadCompleted ? "done" : "failed")}");
        })
        .AddListenerException((_, e) =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERROR: {e.Exception}");
        })
        .LaunchAsync();

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Upgrade completed");
}
catch (Exception ex)
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] FATAL: {ex.Message}");
    Environment.Exit(1);
}

static UpgradeArgs ParseArgs(string[] argv)
{
    var a = new UpgradeArgs();
    for (int i = 0; i < argv.Length; i++)
    {
        switch (argv[i])
        {
            case "--install-path" when i + 1 < argv.Length: a.InstallPath = argv[++i]; break;
            case "--current-version" when i + 1 < argv.Length: a.CurrentVersion = argv[++i]; break;
            case "--target-version" when i + 1 < argv.Length: a.TargetVersion = argv[++i]; break;
            case "--last-version" when i + 1 < argv.Length: a.LastVersion = argv[++i]; break;
            case "--app-secret" when i + 1 < argv.Length: a.AppSecret = argv[++i]; break;
            case "--patch-name" when i + 1 < argv.Length: a.PatchName = argv[++i]; break;
            case "--hash" when i + 1 < argv.Length: a.Hash = argv[++i]; break;
        }
    }
    return a;
}

class UpgradeArgs
{
    public string InstallPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
    public string CurrentVersion { get; set; } = "1.0.0.0";
    public string TargetVersion { get; set; } = "2.0.0.0";
    public string LastVersion { get; set; } = "1.0.0.0";
    public string AppSecret { get; set; } = "";
    public string PatchName { get; set; } = "";
    public string Hash { get; set; } = "";
}
