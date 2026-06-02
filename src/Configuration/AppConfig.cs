using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeneralUpdate.Tools.Configuration;

/// <summary>
/// Top-level application configuration model.
/// Persisted to <c>%APPDATA%/GeneralUpdate.Tools/config.json</c>.
/// </summary>
public class AppConfig
{
    /// <summary>Configuration schema version, for migration support.</summary>
    [JsonProperty("_schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    // ── UI Preferences ────────────────────────────────────────

    [JsonProperty("language")]
    public string Language { get; set; } = "zh-CN";

    [JsonProperty("theme")]
    public string Theme { get; set; } = "Light";

    [JsonProperty("windowWidth")]
    public double WindowWidth { get; set; } = 960;

    [JsonProperty("windowHeight")]
    public double WindowHeight { get; set; } = 640;

    [JsonProperty("windowMaximized")]
    public bool WindowMaximized { get; set; }

    // ── Path Memory ───────────────────────────────────────────

    [JsonProperty("lastPatchOldDir")]
    public string LastPatchOldDir { get; set; } = string.Empty;

    [JsonProperty("lastPatchNewDir")]
    public string LastPatchNewDir { get; set; } = string.Empty;

    [JsonProperty("lastPatchOutputDir")]
    public string LastPatchOutputDir { get; set; } = string.Empty;

    [JsonProperty("lastSimulateAppDir")]
    public string LastSimulateAppDir { get; set; } = string.Empty;

    [JsonProperty("lastSimulatePatchFile")]
    public string LastSimulatePatchFile { get; set; } = string.Empty;

    [JsonProperty("lastSimulateOutputDir")]
    public string LastSimulateOutputDir { get; set; } = string.Empty;

    [JsonProperty("lastConfigClientPath")]
    public string LastConfigClientPath { get; set; } = string.Empty;

    [JsonProperty("lastConfigUpgradePath")]
    public string LastConfigUpgradePath { get; set; } = string.Empty;

    [JsonProperty("lastOssOutputDir")]
    public string LastOssOutputDir { get; set; } = string.Empty;

    [JsonProperty("lastExtensionDir")]
    public string LastExtensionDir { get; set; } = string.Empty;

    [JsonProperty("lastExtensionOutputDir")]
    public string LastExtensionOutputDir { get; set; } = string.Empty;

    // ── Upload Configuration ──────────────────────────────────

    [JsonProperty("uploadServerUrl")]
    public string UploadServerUrl { get; set; } = string.Empty;

    [JsonProperty("uploadEndpoint")]
    public string UploadEndpoint { get; set; } = "/api/v1/packages/upload";

    [JsonProperty("uploadTimeoutSeconds")]
    public int UploadTimeoutSeconds { get; set; } = 300;

    [JsonProperty("uploadRetryCount")]
    public int UploadRetryCount { get; set; } = 3;

    [JsonProperty("autoUploadEnabled")]
    public bool AutoUploadEnabled { get; set; }

    [JsonProperty("uploadAuth")]
    public AuthCredential UploadAuth { get; set; } = new();

    // ── Simulation Configuration ──────────────────────────────

    [JsonProperty("simulationServerPort")]
    public string SimulationServerPort { get; set; } = "5000";

    [JsonProperty("simulationRequireAuth")]
    public bool SimulationRequireAuth { get; set; }

    [JsonProperty("simulationAuth")]
    public AuthCredential SimulationAuth { get; set; } = new();

    [JsonProperty("simulationPlatformType")]
    public string SimulationPlatformType { get; set; } = "Windows";

    [JsonProperty("simulationAppType")]
    public string SimulationAppType { get; set; } = "Client";

    // ── Feature Switches ──────────────────────────────────────

    [JsonProperty("encryptionScanEnabled")]
    public bool EncryptionScanEnabled { get; set; } = true;

    [JsonProperty("autoValidateSemver")]
    public bool AutoValidateSemver { get; set; } = true;

    [JsonProperty("showJsonPreview")]
    public bool ShowJsonPreview { get; set; } = true;
}
