using System.Text.Json;
using GeneralUpdate.Tools.Models;

namespace GeneralUpdate.Tools.Services;

public static class ManifestGeneratorService
{
    public static string GenerateJson(ManifestModel model)
    {
        var obj = new
        {
            mainAppName = model.MainAppName,
            clientVersion = model.ClientVersion,
            appType = model.AppType,
            updateAppName = model.UpdateAppName,
            upgradeClientVersion = model.UpgradeClientVersion,
            productId = model.ProductId,
            updatePath = model.UpdatePath
        };

        return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
    }

    public static ManifestModel FromCsprojInfo(CsprojInfo client, CsprojInfo? upgrade, ManifestModel? userInput = null)
    {
        return new ManifestModel
        {
            MainAppName = userInput?.MainAppName ?? client.AssemblyName,
            ClientVersion = userInput?.ClientVersion ?? "",
            AppType = userInput?.AppType ?? "Client",
            UpdateAppName = userInput?.UpdateAppName ?? upgrade?.AssemblyName ?? "Update.exe",
            UpgradeClientVersion = userInput?.UpgradeClientVersion ?? "",
            ProductId = userInput?.ProductId ?? "",
            UpdatePath = userInput?.UpdatePath ?? "update/"
        };
    }
}
