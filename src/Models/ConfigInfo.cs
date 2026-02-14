using Newtonsoft.Json;

namespace GeneralUpdate.Tool.Avalonia.Models;

/// <summary>
/// Configuration information for package serialization
/// </summary>
public class ConfigInfo
{
    [JsonProperty("reportUrl")]
    public string ReportUrl { get; set; } = string.Empty;

    [JsonProperty("updateUrl")]
    public string UpdateUrl { get; set; } = string.Empty;

    [JsonProperty("appName")]
    public string AppName { get; set; } = string.Empty;

    [JsonProperty("mainAppName")]
    public string MainAppName { get; set; } = string.Empty;

    [JsonProperty("clientVersion")]
    public string ClientVersion { get; set; } = string.Empty;

    [JsonProperty("packetName")]
    public string PacketName { get; set; } = string.Empty;

    [JsonProperty("format")]
    public string Format { get; set; } = string.Empty;

    [JsonProperty("encoding")]
    public string Encoding { get; set; } = string.Empty;
}
