using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GeneralUpdate.Tools.Models;

namespace GeneralUpdate.Tools.Services;

/// <summary>
/// Generates single-file client.cs and upgrade.cs for simulation,
/// using dotnet-run (#r nuget:...) without project files.
/// </summary>
public class ClientGeneratorService
{
    private const string ClientTemplate = """
#r "nuget: GeneralUpdate.ClientCore"
#r "nuget: GeneralUpdate.Core"

using GeneralUpdate.ClientCore;
using GeneralUpdate.Common.Shared.Object;
using GeneralUpdate.Common.Internal.Event;

var log = (string msg) => Console.WriteLine($"[{{DateTime.Now:HH:mm:ss}}] {{msg}}");

try
{{
    log("Client started");
    log("Install path: {0}");

    var config = new Configinfo
    {{
        ReportUrl = "{1}/Upgrade/Report",
        UpdateUrl = "{1}/Upgrade/Verification",
        AppName = "{2}",
        MainAppName = "{3}",
        InstallPath = @"{0}",
        ClientVersion = "{4}",
        UpgradeClientVersion = "{5}",
        ProductId = "{6}",
        AppSecretKey = "{7}",
    }};

    await new GeneralClientBootstrap()
        .SetConfig(config)
        .AddListenerMultiDownloadStatistics((_, e) =>
        {{
            var v = e.Version as VersionInfo;
            log($"Download: {{v?.Version}} {{e.ProgressPercentage}}% {{e.Speed}}/s");
        }})
        .AddListenerMultiAllDownloadCompleted((_, e) =>
        {{
            log(e.IsAllDownloadCompleted ? "All downloads completed" : $"Download failed: {{e.FailedVersions.Count}}");
        }})
        .AddListenerException((_, e) =>
        {{
            log($"ERROR: {{e.Exception}}");
        }})
        .AddListenerUpdateInfo((_, e) =>
        {{
            log($"Update info: Code={{e.Info.Code}}, Versions={{e.Info.Body?.Count ?? 0}}");
        }})
        .LaunchAsync();

    log("Update process completed");
}}
catch (Exception ex)
{{
    log($"FATAL: {{ex.Message}}");
    Console.Error.WriteLine(ex);
    Environment.Exit(1);
}}
""";

    private const string UpgradeTemplate = """
#r "nuget: GeneralUpdate.Core"
#r "nuget: GeneralUpdate.ClientCore"

using GeneralUpdate.Core;
using GeneralUpdate.Common.Shared;
using GeneralUpdate.Common.Internal.Event;

var log = (string msg) => Console.WriteLine($"[{{DateTime.Now:HH:mm:ss}}] {{msg}}");

try
{{
    log("Upgrade process started");
    log("Working directory: " + Environment.CurrentDirectory);

    await new GeneralUpdateBootstrap()
        .AddListenerMultiDownloadStatistics((_, e) =>
        {{
            var v = e.Version as GeneralUpdate.Common.Shared.Object.VersionInfo;
            log($"Download: {{v?.Version}} {{e.ProgressPercentage}}%");
        }})
        .AddListenerMultiAllDownloadCompleted((_, e) =>
        {{
            log(e.IsAllDownloadCompleted ? "Downloads done" : "Download failed");
        }})
        .AddListenerException((_, e) =>
        {{
            log($"ERROR: {{e.Exception}}");
        }})
        .LaunchAsync();

    log("Upgrade process finished successfully");
}}
catch (Exception ex)
{{
    log($"FATAL: {{ex.Message}}");
    Console.Error.WriteLine(ex);
    Environment.Exit(1);
}}
""";

    public async Task GenerateAsync(SimulateConfigModel config, string outputDir)
    {
        var serverUrl = $"http://127.0.0.1:{config.ServerPort}";

        // client.cs
        var clientCode = string.Format(ClientTemplate,
            EscapeForCSharp(config.AppDirectory),
            serverUrl,
            "upgrade.cs",              // AppName - the upgrade process
            "client.cs",               // MainAppName
            config.CurrentVersion,
            "1.0.0.0",                 // upgrade client version
            config.ProductId,
            config.AppSecretKey);

        await File.WriteAllTextAsync(
            Path.Combine(outputDir, "client.cs"),
            clientCode,
            Encoding.UTF8);

        // upgrade.cs
        var upgradeCode = string.Format(UpgradeTemplate,
            EscapeForCSharp(config.AppDirectory));

        await File.WriteAllTextAsync(
            Path.Combine(outputDir, "upgrade.cs"),
            upgradeCode,
            Encoding.UTF8);
    }

    private static string EscapeForCSharp(string s) =>
        s.Replace(@"\", @"\\").Replace("\"", "\\\"");
}
