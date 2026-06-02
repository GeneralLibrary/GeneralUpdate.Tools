namespace GeneralUpdate.Tools.Configuration;

/// <summary>
/// Authentication scheme for server communication.
/// </summary>
public enum AuthScheme
{
    /// <summary>No authentication required.</summary>
    None,

    /// <summary>HTTP Basic Authentication (username + password encoded in Authorization header).</summary>
    Basic,

    /// <summary>Bearer Token authentication (JWT or opaque token in Authorization header).</summary>
    BearerToken,

    /// <summary>Custom API Key in a configurable HTTP header (e.g. X-API-Key).</summary>
    ApiKey,
}
