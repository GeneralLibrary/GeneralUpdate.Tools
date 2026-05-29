using System.Threading;
using System.Threading.Tasks;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.Pipeline.Steps;

public class ManifestBuildStep : IConfigStep
{
    public string Name => "Build manifest";

    public Task<PipelineContext> ExecuteAsync(PipelineContext ctx, CancellationToken ct)
    {
        // Manifest already populated from user input via ConfigGeneratorModel.
        // If auto-analysis was used, fill in AssemblyName suggestions from client/upgrade info.
        if (ctx.ClientInfo != null && string.IsNullOrWhiteSpace(ctx.Manifest.MainAppName))
            ctx.Manifest.MainAppName = ctx.ClientInfo.AssemblyName;

        if (ctx.UpgradeInfo != null && string.IsNullOrWhiteSpace(ctx.Manifest.UpdateAppName))
            ctx.Manifest.UpdateAppName = ctx.UpgradeInfo.AssemblyName;

        return Task.FromResult(ctx);
    }
}
