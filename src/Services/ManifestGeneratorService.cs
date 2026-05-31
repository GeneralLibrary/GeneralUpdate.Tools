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

    /// <summary>
    ///     Builds a <see cref="ManifestModel"/> from parsed csproj data,
    ///     with user input overriding where a non-blank value was supplied.
    ///     Uses <see cref="string.IsNullOrWhiteSpace"/> so that empty strings
    ///     (the default for unedited UI fields) still fall back to csproj data.
    /// </summary>
    public static ManifestModel FromCsprojInfo(CsprojInfo client, CsprojInfo? upgrade, ManifestModel? userInput = null)
    {
        return new ManifestModel
        {
            MainAppName = !string.IsNullOrWhiteSpace(userInput?.MainAppName)
                ? userInput.MainAppName
                : client.AssemblyName,
            ClientVersion = userInput?.ClientVersion ?? "",
            AppType = !string.IsNullOrWhiteSpace(userInput?.AppType)
                ? userInput.AppType
                : "Client",
            UpdateAppName = !string.IsNullOrWhiteSpace(userInput?.UpdateAppName)
                ? userInput.UpdateAppName
                : upgrade?.AssemblyName ?? "Update.exe",
            UpgradeClientVersion = userInput?.UpgradeClientVersion ?? "",
            ProductId = userInput?.ProductId ?? "",
            UpdatePath = !string.IsNullOrWhiteSpace(userInput?.UpdatePath)
                ? userInput.UpdatePath
                : "update/"
        };
    }
}
