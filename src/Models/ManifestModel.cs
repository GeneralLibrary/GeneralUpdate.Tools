namespace GeneralUpdate.Tools.Models;

public class ManifestModel
{
    public string MainAppName { get; set; } = "";
    public string ClientVersion { get; set; } = "1.0.0";
    public string AppType { get; set; } = "Client";
    public string UpdateAppName { get; set; } = "Update.exe";
    public string UpgradeClientVersion { get; set; } = "1.0.0";
    public string ProductId { get; set; } = "";
    public string UpdatePath { get; set; } = "update/";
}
