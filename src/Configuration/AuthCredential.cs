using System;

namespace GeneralUpdate.Tools.Configuration;

/// <summary>
/// Generic authentication credential used for both upload and simulation scenarios.
/// Sensitive fields (passwords, tokens, API keys) are stored encrypted via DPAPI.
/// </summary>
public class AuthCredential
{
    /// <summary>Authentication scheme to use.</summary>
    public AuthScheme Scheme { get; set; } = AuthScheme.None;

    // ── Basic Auth ────────────────────────────────────────────

    /// <summary>Username for Basic Authentication.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>DPAPI-encrypted password for Basic Authentication.</summary>
    public string EncryptedPassword { get; set; } = string.Empty;

    // ── Bearer Token ──────────────────────────────────────────

    /// <summary>DPAPI-encrypted bearer token / JWT.</summary>
    public string EncryptedToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional login endpoint URL. When set, the upload service can
    /// automatically obtain a fresh token by POSTing credentials to this endpoint.
    /// </summary>
    public string LoginUrl { get; set; } = string.Empty;

    // ── API Key ───────────────────────────────────────────────

    /// <summary>Custom HTTP header name for API Key (e.g. "X-API-Key").</summary>
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";

    /// <summary>DPAPI-encrypted API key value.</summary>
    public string EncryptedApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Repair invalid enum values that may result from manual JSON editing
    /// or unknown future scheme values.
    /// </summary>
    internal void Sanitize()
    {
        if (!Enum.IsDefined(typeof(AuthScheme), Scheme))
            Scheme = AuthScheme.None;
    }
}
