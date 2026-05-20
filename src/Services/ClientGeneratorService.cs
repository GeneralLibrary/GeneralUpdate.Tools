using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GeneralUpdate.Tools.Models;

namespace GeneralUpdate.Tools.Services;

/// <summary>
/// Generates client and upgrade console projects for simulation,
/// each with a minimal .csproj + Program.cs using dotnet run --project.
/// </summary>
public class ClientGeneratorService
{
    private const string ClientCsproj = """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="GeneralUpdate.ClientCore" Version="10.*" />
    <PackageReference Include="GeneralUpdate.Core" Version="10.*" />
  </ItemGroup>
</Project>
""";

    private const string ClientProgram = """
var log = (string msg) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");

try
{
    log("Client started");
    log("Install path: {0}");

    var config = new GeneralUpdate.Common.Shared.Object.Configinfo
    {
        ReportUrl = "{1}/Upgrade/Report",
        UpdateUrl = "{1}/Upgrade/Verification",
        AppName = "{2}",
        MainAppName = "{3}",
        InstallPath = @"{0}",
        ClientVersion = "{4}",
        UpgradeClientVersion = "{5}",
        ProductId = "{6}",
        AppSecretKey = "{7}",
    };

    await new GeneralUpdate.ClientCore.GeneralClientBootstrap()
        .SetConfig(config)
        .AddListenerMultiDownloadStatistics((_, e) =>
        {
            var v = e.Version as GeneralUpdate.Common.Shared.Object.VersionInfo;
            log($"Download: {v?.Version} {e.ProgressPercentage}% {e.Speed}/s");
        })
        .AddListenerMultiAllDownloadCompleted((_, e) =>
        {
            log(e.IsAllDownloadCompleted ? "All downloads completed" : $"Download failed: {e.FailedVersions.Count}");
        })
        .AddListenerException((_, e) =>
        {
            log($"ERROR: {e.Exception}");
        })
        .AddListenerUpdateInfo((_, e) =>
        {
            log($"Update info: Code={e.Info.Code}, Versions={e.Info.Body?.Count ?? 0}");
        })
        .LaunchAsync();

    log("Update process completed");
}
catch (Exception ex)
{
    log($"FATAL: {ex.Message}");
    Console.Error.WriteLine(ex);
    Environment.Exit(1);
}
""";

    private const string UpgradeCsproj = """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="GeneralUpdate.Core" Version="10.*" />
    <PackageReference Include="GeneralUpdate.ClientCore" Version="10.*" />
  </ItemGroup>
</Project>
""";

    private const string UpgradeProgram = """
var log = (string msg) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");

try
{
    log("Upgrade process started");
    log("Working directory: " + Environment.CurrentDirectory);

    await new GeneralUpdate.Core.GeneralUpdateBootstrap()
        .AddListenerMultiDownloadStatistics((_, e) =>
        {
            var v = e.Version as GeneralUpdate.Common.Shared.Object.VersionInfo;
            log($"Download: {v?.Version} {e.ProgressPercentage}%");
        })
        .AddListenerMultiAllDownloadCompleted((_, e) =>
        {
            log(e.IsAllDownloadCompleted ? "Downloads done" : "Download failed");
        })
        .AddListenerException((_, e) =>
        {
            log($"ERROR: {e.Exception}");
        })
        .LaunchAsync();

    log("Upgrade process finished successfully");
}
catch (Exception ex)
{
    log($"FATAL: {ex.Message}");
    Console.Error.WriteLine(ex);
    Environment.Exit(1);
}
""";

    public async Task GenerateAsync(SimulateConfigModel config, string outputDir)
    {
        var serverUrl = $"http://127.0.0.1:{config.ServerPort}";

        // client/
        var clientDir = Path.Combine(outputDir, "client");
        Directory.CreateDirectory(clientDir);
        await File.WriteAllTextAsync(Path.Combine(clientDir, "client.csproj"), ClientCsproj, Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(clientDir, "Program.cs"),
            string.Format(ClientProgram,
                EscapeForCSharp(config.AppDirectory),
                serverUrl,
                "upgrade.exe",              // AppName
                "client.exe",               // MainAppName
                config.CurrentVersion,
                "1.0.0.0",                  // upgrade client version
                config.ProductId,
                config.AppSecretKey),
            Encoding.UTF8);

        // upgrade/
        var upgradeDir = Path.Combine(outputDir, "upgrade");
        Directory.CreateDirectory(upgradeDir);
        await File.WriteAllTextAsync(Path.Combine(upgradeDir, "upgrade.csproj"), UpgradeCsproj, Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(upgradeDir, "Program.cs"),
            string.Format(UpgradeProgram,
                EscapeForCSharp(config.AppDirectory)),
            Encoding.UTF8);
    }

    private static string EscapeForCSharp(string s) =>
        s.Replace(@"\", @"\\").Replace("\"", "\\\"");
}
