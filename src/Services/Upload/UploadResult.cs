namespace GeneralUpdate.Tools.Services;

/// <summary>
/// Result of an upload operation.
/// </summary>
public class UploadResult
{
    /// <summary>Whether the upload succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Error message if the upload failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Server response body (may be empty).</summary>
    public string? ServerResponse { get; init; }

    /// <summary>HTTP status code returned by the server.</summary>
    public int StatusCode { get; init; }

    /// <summary>URL the package was uploaded to.</summary>
    public string UploadUrl { get; init; } = string.Empty;

    public static UploadResult Ok(int statusCode, string? response, string url)
        => new() { Success = true, StatusCode = statusCode, ServerResponse = response, UploadUrl = url };

    public static UploadResult Fail(string error, int statusCode = 0)
        => new() { Success = false, ErrorMessage = error, StatusCode = statusCode };
}
