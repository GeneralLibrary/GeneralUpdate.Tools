using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.Pipeline.Steps;

public class FileEmitStep : IConfigStep
{
    private readonly string? _outputDir;

    public FileEmitStep(string? outputDir = null)
    {
        _outputDir = outputDir;
    }

    public string Name => "Emit manifest file";

    public Task<PipelineContext> ExecuteAsync(PipelineContext ctx, CancellationToken ct)
    {
        var json = ManifestGeneratorService.GenerateJson(ctx.Manifest);

        var dir = _outputDir ?? AppContext.BaseDirectory;
        if (string.IsNullOrWhiteSpace(dir))
        {
            ctx.Errors.Add("No output directory available to write manifest.");
            return Task.FromResult(ctx);
        }

        var path = Path.Combine(dir, "generalupdate.manifest.json");
        try
        {
            File.WriteAllText(path, json);
        }
        catch (System.Exception ex)
        {
            ctx.Errors.Add($"Failed to write manifest to {path}: {ex.Message}");
        }

        return Task.FromResult(ctx);
    }
}
