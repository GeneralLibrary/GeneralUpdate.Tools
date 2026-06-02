using System;

namespace GeneralUpdate.Tools.Services;

/// <summary>
/// Upload progress event arguments.
/// </summary>
public class UploadProgressEventArgs : EventArgs
{
    /// <summary>Bytes uploaded so far.</summary>
    public long BytesUploaded { get; init; }

    /// <summary>Total bytes to upload.</summary>
    public long TotalBytes { get; init; }

    /// <summary>Upload progress as a percentage (0–100).</summary>
    public double Percentage => TotalBytes > 0 ? (double)BytesUploaded / TotalBytes * 100 : 0;

    /// <summary>Current phase of the upload.</summary>
    public UploadPhase Phase { get; init; }

    /// <summary>Optional status message.</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Phases of an upload operation.
/// </summary>
public enum UploadPhase
{
    Connecting,
    Authenticating,
    Uploading,
    Verifying,
    Completed,
    Failed,
}
