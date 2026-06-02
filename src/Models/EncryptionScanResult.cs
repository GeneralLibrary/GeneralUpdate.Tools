using System.Collections.Generic;

namespace GeneralUpdate.Tools.Models;

public enum RiskLevel
{
    /// <summary>Statistically anomalous but no explicit signature — informational only.</summary>
    Low,

    /// <summary>High entropy or structural anomaly — worth reviewing.</summary>
    Medium,

    /// <summary>Known protector signature or encryption container — strongly suggest exclusion.</summary>
    High
}

public enum DetectionMethod
{
    ExtensionBlacklist,
    PeProtectorSection,
    PeClrHeader,
    PeSectionEntropy,
    ElfSectionEntropy,
    JarClassMagic,
    FullFileEntropy
}

public class SuspiciousFile
{
    public string RelativePath { get; set; } = "";
    public string FilePath { get; set; } = "";
    public RiskLevel Level { get; set; }
    public string Reason { get; set; } = "";
    public double Entropy { get; set; }
    public DetectionMethod Method { get; set; }
    public string? DetectionDetail { get; set; }
}

public class EncryptionScanResult
{
    public List<SuspiciousFile> SuspiciousFiles { get; set; } = new();
    public int TotalFilesScanned { get; set; }
    public bool HasSuspiciousFiles => SuspiciousFiles.Count > 0;
    public bool HasHighRisk => SuspiciousFiles.Exists(f => f.Level == RiskLevel.High);
}
