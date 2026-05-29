using System.Collections.Generic;
using GeneralUpdate.Tools.Models;

namespace GeneralUpdate.Tools.Pipeline;

public class PipelineContext
{
    public string ClientPath { get; set; } = "";
    public string? UpgradePath { get; set; }
    public CsprojInfo? ClientInfo { get; set; }
    public CsprojInfo? UpgradeInfo { get; set; }
    public ManifestModel Manifest { get; set; } = new();
    public List<string> Errors { get; } = new();
    public bool IsCli { get; set; }
    public bool Force { get; set; }
}
