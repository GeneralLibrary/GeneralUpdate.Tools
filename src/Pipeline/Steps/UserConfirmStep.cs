using System.Threading;
using System.Threading.Tasks;

namespace GeneralUpdate.Tools.Pipeline.Steps;

/// <summary>
///     In GUI mode, the user confirms via the ViewModel before the pipeline runs,
///     so this step is a no-op. In CLI mode with --force, it also passes through.
///     Without --force in CLI mode, this step would block for interactive input
///     (reserved for future CLI integration).
/// </summary>
public class UserConfirmStep : IConfigStep
{
    public string Name => "User confirm";

    public Task<PipelineContext> ExecuteAsync(PipelineContext ctx, CancellationToken ct)
    {
        if (ctx.IsCli && !ctx.Force)
            ctx.Errors.Add("CLI mode requires --force to skip confirmation.");

        return Task.FromResult(ctx);
    }
}
