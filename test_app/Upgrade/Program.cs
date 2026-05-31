using GeneralUpdate.Core;
using GeneralUpdate.Core.Configuration;
using GeneralUpdate.Core.Download;
using GeneralUpdate.Core.Event;

try
{
    Console.WriteLine($"=== GeneralUpdate Simulation Upgrade ===");
    Console.WriteLine($"Started: {DateTime.Now}");
    Console.WriteLine($"Running from: {AppDomain.CurrentDomain.BaseDirectory}");

    // Config comes from the encrypted IPC file written by the Client process.
    // The Client's generalupdate.manifest.json flows through IPC,
    // so the Upgrade never needs to load a manifest directly.
    await new GeneralUpdateBootstrap()
        .SetOption(Option.AppType, AppType.Upgrade)
        .AddListenerMultiDownloadStatistics(OnDownloadStatistics)
        .AddListenerMultiDownloadCompleted(OnDownloadCompleted)
        .AddListenerMultiAllDownloadCompleted(OnAllDownloadCompleted)
        .AddListenerMultiDownloadError(OnDownloadError)
        .AddListenerException(OnException)
        .LaunchAsync();

    Console.WriteLine("Upgrade test completed.");
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL: {ex}");
    Environment.Exit(1);
}

static void OnDownloadStatistics(object sender, MultiDownloadStatisticsEventArgs e)
{
    var v = e.Version as VersionEntry;
    Console.WriteLine($"[Apply] {v?.Version}: {e.ProgressPercentage}%");
}

static void OnDownloadCompleted(object sender, MultiDownloadCompletedEventArgs e)
{
    var v = e.Version as VersionEntry;
    Console.WriteLine($"[Apply] {v?.Version}: {(e.IsCompleted ? "SUCCESS" : "FAILED")}");
}

static void OnAllDownloadCompleted(object sender, MultiAllDownloadCompletedEventArgs e)
{
    Console.WriteLine(e.IsAllDownloadCompleted
        ? "[Apply] All patches applied."
        : $"[Apply] Patches finished with {e.FailedVersions.Count} failure(s).");
}

static void OnDownloadError(object sender, MultiDownloadErrorEventArgs e)
{
    var v = e.Version as VersionEntry;
    Console.WriteLine($"[Apply] Error @ {v?.Version}: {e.Exception.Message}");
}

static void OnException(object sender, ExceptionEventArgs e)
{
    Console.WriteLine($"[Error] {e.Exception}");
}
