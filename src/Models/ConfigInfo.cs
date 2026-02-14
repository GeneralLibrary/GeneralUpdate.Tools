using Newtonsoft.Json;

namespace GeneralUpdate.Tool.Avalonia.Models;

/// <summary>
/// Configuration information for package serialization
/// </summary>
public class ConfigInfo
{
    [JsonProperty("reportUrl")]
    public string ReportUrl { get; set; }

    [JsonProperty("updateUrl")]
    public string UpdateUrl { get; set; }

    [JsonProperty("appName")]
    public string AppName { get; set; }

    [JsonProperty("mainAppName")]
    public string MainAppName { get; set; }

    [JsonProperty("clientVersion")]
    public string ClientVersion { get; set; }

    [JsonProperty("packetName")]
    public string PacketName { get; set; }

    [JsonProperty("format")]
    public string Format { get; set; }

    [JsonProperty("encoding")]
    public string Encoding { get; set; }
}
