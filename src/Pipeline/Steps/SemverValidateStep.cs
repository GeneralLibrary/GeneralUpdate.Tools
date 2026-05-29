using System.Threading;
using System.Threading.Tasks;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.Pipeline.Steps;

public class SemverValidateStep : IConfigStep
{
    public string Name => "Validate semver";

    public Task<PipelineContext> ExecuteAsync(PipelineContext ctx, CancellationToken ct)
    {
        var m = ctx.Manifest;
        Validate(m.ClientVersion, nameof(m.ClientVersion), ctx);
        Validate(m.UpgradeClientVersion, nameof(m.UpgradeClientVersion), ctx);
        return Task.FromResult(ctx);
    }

    internal static void Validate(string? version, string fieldName, PipelineContext ctx)
    {
        if (string.IsNullOrWhiteSpace(version))
            ctx.Errors.Add($"{fieldName} is required and must follow semver (e.g. 1.0.0).");
        else if (!SemverValidator.IsValid(version))
            ctx.Errors.Add($"{fieldName} '{version}' does not follow semver (https://semver.org).");
    }
}
