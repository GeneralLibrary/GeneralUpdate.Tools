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

    // ── Mobile / Android Path Memory ───────────────────────────

    [JsonProperty("lastMobileFilePath")]
    public string LastMobileFilePath { get; set; } = string.Empty;

    [JsonProperty("lastMobileProjectPath")]
    public string LastMobileProjectPath { get; set; } = string.Empty;

    [JsonProperty("lastMobileOutputDir")]
    public string LastMobileOutputDir { get; set; } = string.Empty;

    [JsonProperty("lastMobileProductId")]
    public string LastMobileProductId { get; set; } = string.Empty;

    [JsonProperty("lastMobilePlatform")]
    public int LastMobilePlatform { get; set; } = 4;

    [JsonProperty("lastMobileUseProjectMode")]
    public bool LastMobileUseProjectMode { get; set; } = true;

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

    // ── Sanitization ───────────────────────────────────────────

    /// <summary>
    /// Repair invalid values that may have been introduced by manual JSON editing
    /// or deserialization of an unknown/invalid enum value.
    /// Called automatically by <see cref="ConfigService.Load"/> after deserialization.
    /// </summary>
    internal void Sanitize()
    {
        // Repair null nested objects (should never be null, but guard against manual JSON edits)
        UploadAuth ??= new AuthCredential();
        SimulationAuth ??= new AuthCredential();

        // Validate numeric ranges
        if (UploadTimeoutSeconds < 10)
            UploadTimeoutSeconds = 300;
        if (UploadRetryCount is < 0 or > 10)
            UploadRetryCount = 3;

        // Repair invalid enum values in auth credentials
        UploadAuth.Sanitize();
        SimulationAuth.Sanitize();
    }
}
