using System.Threading;
using System.Threading.Tasks;

namespace GeneralUpdate.Tools.Pipeline;

public interface IConfigStep
{
    string Name { get; }
    Task<PipelineContext> ExecuteAsync(PipelineContext ctx, CancellationToken ct);
}
