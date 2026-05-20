using System.Diagnostics;
using GeneralUpdate.ClientCore;
using GeneralUpdate.Common.Download;
using GeneralUpdate.Common.Internal;
using GeneralUpdate.Common.Internal.Bootstrap;
using GeneralUpdate.Common.Shared.Object;

// Parse command-line arguments
var cliArgs = ParseArgs(args);

Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Client started");
Console.WriteLine($"Install path: {cliArgs.InstallPath}");

var configinfo = new Configinfo
{
    ReportUrl = $"{cliArgs.ServerUrl}/Upgrade/Report",
    UpdateUrl = $"{cliArgs.ServerUrl}/Upgrade/Verification",
    AppName = cliArgs.AppName,
    MainAppName = AppDomain.CurrentDomain.FriendlyName,
    InstallPath = cliArgs.InstallPath,
    ClientVersion = cliArgs.CurrentVersion,
    UpgradeClientVersion = "1.0.0.0",
    ProductId = cliArgs.ProductId,
    AppSecretKey = cliArgs.AppSecret,
};

try
{
    await new GeneralClientBootstrap()
        .AddListenerMultiDownloadStatistics((_, e) =>
        {
            var v = e.Version as VersionInfo;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Download: {v?.Version} {e.ProgressPercentage}%");
        })
        .AddListenerMultiAllDownloadCompleted((_, e) =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] All downloads: {(e.IsAllDownloadCompleted ? "completed" : $"failed ({e.FailedVersions.Count})")}");
        })
        .AddListenerMultiDownloadCompleted((_, e) =>
        {
            var v = e.Version as VersionInfo;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Download {v?.Version}: {(e.IsComplated ? "done" : "failed")}");
        })
        .AddListenerException((_, e) =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERROR: {e.Exception}");
        })
        .AddListenerUpdateInfo((_, e) =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Update info: Code={e.Info.Code}, Versions={e.Info.Body?.Count ?? 0}");
        })
        .SetConfig(configinfo)
        .Option(UpdateOption.DownloadTimeOut, 60)
        .LaunchAsync();

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Client completed successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] FATAL: {ex.Message}");
    Environment.Exit(1);
}

static ClientArgs ParseArgs(string[] argv)
{
    var a = new ClientArgs();
    for (int i = 0; i < argv.Length; i++)
    {
        switch (argv[i])
        {
            case "--server-url" when i + 1 < argv.Length: a.ServerUrl = argv[++i]; break;
            case "--install-path" when i + 1 < argv.Length: a.InstallPath = argv[++i]; break;
            case "--current-version" when i + 1 < argv.Length: a.CurrentVersion = argv[++i]; break;
            case "--app-secret" when i + 1 < argv.Length: a.AppSecret = argv[++i]; break;
            case "--product-id" when i + 1 < argv.Length: a.ProductId = argv[++i]; break;
            case "--app-name" when i + 1 < argv.Length: a.AppName = argv[++i]; break;
        }
    }
    return a;
}

class ClientArgs
{
    public string ServerUrl { get; set; } = "http://127.0.0.1:5000";
    public string InstallPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
    public string CurrentVersion { get; set; } = "1.0.0.0";
    public string AppSecret { get; set; } = "dfeb5833-975e-4afb-88f1-6278ee9aeff6";
    public string ProductId { get; set; } = "2d974e2a-31e6-4887-9bb1-b4689e98c77a";
    public string AppName { get; set; } = "Upgrade.exe";
}
