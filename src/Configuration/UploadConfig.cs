namespace GeneralUpdate.Tools.Configuration;

/// <summary>
/// Runtime upload configuration, populated from <see cref="AppConfig"/>.
/// Used by <see cref="Services.HttpUploadService"/>.
/// </summary>
public class UploadConfig
{
    public string ServerUrl { get; set; } = string.Empty;
    public string UploadEndpoint { get; set; } = "/api/v1/packages/upload";
    public int TimeoutSeconds { get; set; } = 300;
    public int RetryCount { get; set; } = 3;
    public AuthCredential Auth { get; set; } = new();
    public bool AutoUploadEnabled { get; set; }
}
