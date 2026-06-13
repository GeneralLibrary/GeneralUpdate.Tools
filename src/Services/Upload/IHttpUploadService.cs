using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeneralUpdate.Tools.Configuration;

namespace GeneralUpdate.Tools.Services;

/// <summary>
/// Service for uploading packages to a remote HTTP server.
/// Supports desktop patch packages and mobile packages (APK/AAB).
/// </summary>
public interface IHttpUploadService
{
    /// <summary>
    /// Upload a file to the configured server endpoint using multipart/form-data.
    /// </summary>
    Task<UploadResult> UploadAsync(
        string filePath,
        UploadConfig config,
        IProgress<UploadProgressEventArgs>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Upload a file with additional form fields (metadata DTO).
    /// Used when the server endpoint expects DTO fields alongside the file.
    /// </summary>
    Task<UploadResult> UploadAsync(
        string filePath,
        UploadConfig config,
        Dictionary<string, string> formFields,
        IProgress<UploadProgressEventArgs>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Test the connection to the configured server and validate authentication.
    /// </summary>
    Task<bool> ValidateConnectionAsync(UploadConfig config, CancellationToken ct = default);
}
