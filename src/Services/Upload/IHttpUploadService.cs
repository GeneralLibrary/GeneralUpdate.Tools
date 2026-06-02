using System;
using System.Threading;
using System.Threading.Tasks;
using GeneralUpdate.Tools.Configuration;

namespace GeneralUpdate.Tools.Services;

/// <summary>
/// Service for uploading patch packages to a remote HTTP server.
/// </summary>
public interface IHttpUploadService
{
    /// <summary>
    /// Upload a file to the configured server endpoint using multipart/form-data.
    /// </summary>
    /// <param name="filePath">Local path to the file to upload.</param>
    /// <param name="config">Upload configuration (server, auth, retry).</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<UploadResult> UploadAsync(
        string filePath,
        UploadConfig config,
        IProgress<UploadProgressEventArgs>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Test the connection to the configured server and validate authentication.
    /// </summary>
    Task<bool> ValidateConnectionAsync(UploadConfig config, CancellationToken ct = default);
}
