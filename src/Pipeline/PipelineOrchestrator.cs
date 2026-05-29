using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GeneralUpdate.Tools.Pipeline;

public class PipelineOrchestrator
{
    private readonly List<IConfigStep> _steps = new();

    public PipelineOrchestrator AddStep(IConfigStep step)
    {
        _steps.Add(step);
        return this;
    }

    public async Task<PipelineContext> RunAsync(PipelineContext ctx, CancellationToken ct = default)
    {
        foreach (var step in _steps)
        {
            ctx = await step.ExecuteAsync(ctx, ct);
        }
        return ctx;
    }
}
