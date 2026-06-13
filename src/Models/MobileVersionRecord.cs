using Newtonsoft.Json;

namespace GeneralUpdate.Tools.Models;

public class MobileVersionRecord
{
    [JsonProperty("name")] public string Name { get; set; } = string.Empty;
    [JsonProperty("version")] public string Version { get; set; } = string.Empty;
    [JsonProperty("hash")] public string Hash { get; set; } = string.Empty;
    [JsonProperty("url")] public string Url { get; set; } = string.Empty;
    [JsonProperty("packageName")] public string PackageName { get; set; } = string.Empty;
    [JsonProperty("fileSize")] public long FileSize { get; set; }
    [JsonProperty("format")] public string Format { get; set; } = string.Empty;
    [JsonProperty("platform")] public int Platform { get; set; } = 4;
    [JsonProperty("appType")] public int AppType { get; set; } = 1;
    [JsonProperty("productId")] public string ProductId { get; set; } = string.Empty;
    [JsonProperty("isForcibly")] public bool IsForcibly { get; set; }
    [JsonProperty("releaseDate")] public string ReleaseDate { get; set; } = string.Empty;
}
