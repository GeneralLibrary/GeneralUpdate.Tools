using System.Threading;
using System.Threading.Tasks;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.Pipeline.Steps;

public class CsprojParseStep : IConfigStep
{
    public string Name => "Parse .csproj";

    public Task<PipelineContext> ExecuteAsync(PipelineContext ctx, CancellationToken ct)
    {
        try
        {
            ctx.ClientInfo = CsprojParserService.Parse(ctx.ClientPath);
            if (ctx.ClientInfo == null)
                ctx.Errors.Add($"Failed to parse Client .csproj: {ctx.ClientPath}");
        }
        catch (System.Exception ex)
        {
            ctx.Errors.Add($"Failed to parse Client .csproj: {ex.Message}");
        }

        if (!string.IsNullOrWhiteSpace(ctx.UpgradePath))
        {
            try
            {
                ctx.UpgradeInfo = CsprojParserService.Parse(ctx.UpgradePath);
                if (ctx.UpgradeInfo == null)
                    ctx.Errors.Add($"Failed to parse Upgrade .csproj: {ctx.UpgradePath}");
            }
            catch (System.Exception ex)
            {
                ctx.Errors.Add($"Failed to parse Upgrade .csproj: {ex.Message}");
            }
        }

        return Task.FromResult(ctx);
    }
}
