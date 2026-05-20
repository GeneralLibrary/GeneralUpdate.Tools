using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GeneralUpdate.Tools.Models;

namespace GeneralUpdate.Tools.Services;

/// <summary>
/// Generates simulation_report.md after a simulation run.
/// </summary>
public class ReportGeneratorService
{
    public async Task<string> GenerateAsync(
        SimulateConfigModel config,
        SimulationResult result,
        string outputDir)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Update Simulation Report");
        sb.AppendLine();
        sb.AppendLine("## Configuration");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|-------|-------|");
        sb.AppendLine($"| Patch | {EscapeMd(config.PatchFilePath)} |");
        sb.AppendLine($"| App Directory | {EscapeMd(config.AppDirectory)} |");
        sb.AppendLine($"| Platform | {config.Platform} |");
        sb.AppendLine($"| AppType | {config.AppType} |");
        sb.AppendLine($"| Version | {config.CurrentVersion} → {config.TargetVersion} |");
        sb.AppendLine($"| Server Port | {config.ServerPort} |");
        sb.AppendLine($"| Simulation Time | {DateTime.Now:yyyy-MM-dd HH:mm:ss} |");
        sb.AppendLine();

        sb.AppendLine("## Result");
        sb.AppendLine();
        sb.AppendLine($"**{(result.Success ? "✅ PASS" : "❌ FAIL")}** — {result.Elapsed.TotalSeconds:F1}s");
        if (!string.IsNullOrEmpty(result.ErrorMessage))
            sb.AppendLine($"\nError: `{result.ErrorMessage}`");
        sb.AppendLine();

        if (result.Notes.Count > 0)
        {
            sb.AppendLine("## Notes");
            sb.AppendLine();
            foreach (var note in result.Notes)
                sb.AppendLine($"- {note}");
            sb.AppendLine();
        }

        sb.AppendLine("## Timeline");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.Append(result.FullLog);
        sb.AppendLine("```");

        var reportPath = Path.Combine(outputDir, "simulation_report.md");
        await File.WriteAllTextAsync(reportPath, sb.ToString(), Encoding.UTF8);
        return reportPath;
    }

    private static string EscapeMd(string s) => s.Replace(@"\", @"\\").Replace("|", "\\|");
}
