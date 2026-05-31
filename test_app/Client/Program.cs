using GeneralUpdate.Core;
using GeneralUpdate.Core.Configuration;
using GeneralUpdate.Core.Download;
using GeneralUpdate.Core.Event;

// Parse command-line arguments (URLs and secrets only — app names are fixed)
var cliArgs = ParseArgs(args);

Console.WriteLine($"=== GeneralUpdate Simulation Client ===");
Console.WriteLine($"Started: {DateTime.Now}");
Console.WriteLine($"Server: {cliArgs.ServerUrl}");
Console.WriteLine($"ClientVersion: {cliArgs.ClientVersion}");

// App names are fixed — they match the dotnet publish output names.
const string mainAppName = "ClientSample.exe";
const string updateAppName = "UpgradeSample.exe";
const string updatePath = "update/";

// Read ClientVersion from manifest first — WriteBackClientVersion updates it
// after a successful patch, so subsequent runs see the latest version.
var manifest = ManifestInfo.Load();
var clientVersion = manifest?.ClientVersion ?? cliArgs.ClientVersion;
Console.WriteLine($"Resolved ClientVersion: {clientVersion} (manifest: {manifest?.ClientVersion}, cli: {cliArgs.ClientVersion})");

try
{
    var updateRequest = new UpdateRequest
    {
        UpdateUrl = $"{cliArgs.ServerUrl}/Upgrade/Verification",
        ReportUrl = $"{cliArgs.ServerUrl}/Upgrade/Report",
        AppSecretKey = cliArgs.AppSecret,
        MainAppName = mainAppName,
        UpdateAppName = updateAppName,
        UpdatePath = updatePath,
        ClientVersion = clientVersion,
    };

    await new GeneralUpdateBootstrap()
        .SetConfig(updateRequest)
        .SetOption(Option.AppType, AppType.Client)
        .SetOption(Option.DownloadTimeout, 60)
        .AddListenerMultiDownloadStatistics(OnDownloadStatistics)
        .AddListenerMultiDownloadCompleted(OnDownloadCompleted)
        .AddListenerMultiAllDownloadCompleted(OnAllDownloadCompleted)
        .AddListenerMultiDownloadError(OnDownloadError)
        .AddListenerException(OnException)
        .AddListenerUpdateInfo(OnUpdateInfo)
        .LaunchAsync();

    Console.WriteLine("Client test completed.");
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL: {ex}");
    Environment.Exit(1);
}

static void OnDownloadStatistics(object sender, MultiDownloadStatisticsEventArgs e)
{
    var v = e.Version as VersionEntry;
    Console.WriteLine($"[Download] {v?.Version}: {e.ProgressPercentage}% | {e.Speed} | ETA: {e.Remaining}");
}

static void OnDownloadCompleted(object sender, MultiDownloadCompletedEventArgs e)
{
    var v = e.Version as VersionEntry;
    Console.WriteLine($"[Download] {v?.Version}: {(e.IsCompleted ? "SUCCESS" : "FAILED")}");
}

static void OnAllDownloadCompleted(object sender, MultiAllDownloadCompletedEventArgs e)
{
    Console.WriteLine(e.IsAllDownloadCompleted
        ? "[Download] All downloads completed."
        : $"[Download] Downloads finished with {e.FailedVersions.Count} failure(s).");
}

static void OnDownloadError(object sender, MultiDownloadErrorEventArgs e)
{
    var v = e.Version as VersionEntry;
    Console.WriteLine($"[Download] Error @ {v?.Version}: {e.Exception.Message}");
}

static void OnException(object sender, ExceptionEventArgs e)
{
    Console.WriteLine($"[Error] {e.Exception}");
}

static void OnUpdateInfo(object sender, UpdateInfoEventArgs e)
{
    Console.WriteLine($"[UpdateInfo] Code={e.Info?.Code}, Message={e.Info?.Message}");
    if (e.Info?.Body is { Count: > 0 })
    {
        foreach (var vi in e.Info.Body)
            Console.WriteLine($"  - {vi.Version} ({vi.Name}) [{vi.Size} bytes]{(vi.IsForcibly == true ? " (forced)" : "")}");
    }
    else
    {
        Console.WriteLine("  No updates available.");
    }
}

static ClientArgs ParseArgs(string[] argv)
{
    var a = new ClientArgs();
    for (int i = 0; i < argv.Length; i++)
    {
        switch (argv[i])
        {
            case "--server-url" when i + 1 < argv.Length: a.ServerUrl = argv[++i]; break;
            case "--app-secret" when i + 1 < argv.Length: a.AppSecret = argv[++i]; break;
            case "--client-version" when i + 1 < argv.Length: a.ClientVersion = argv[++i]; break;
        }
    }
    return a;
}

internal class ClientArgs
{
    public string ServerUrl { get; set; } = "http://127.0.0.1:5000";
    public string AppSecret { get; set; } = "dfeb5833-975e-4afb-88f1-6278ee9aeff6";
    public string ClientVersion { get; set; } = "1.0.0";
}
