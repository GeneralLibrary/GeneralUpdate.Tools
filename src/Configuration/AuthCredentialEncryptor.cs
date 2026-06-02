using System;
using System.Security.Cryptography;
using System.Text;

namespace GeneralUpdate.Tools.Configuration;

/// <summary>
/// Encrypts and decrypts sensitive credential fields using Windows Data Protection API (DPAPI).
/// On non-Windows platforms, falls back to Base64 encoding (not cryptographically secure).
/// </summary>
public static class AuthCredentialEncryptor
{
    private static readonly DataProtectionScope Scope = DataProtectionScope.CurrentUser;

    /// <summary>Encrypt a plain-text secret. Returns Base64-encoded ciphertext.</summary>
    public static string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = ProtectedData.Protect(plainBytes, null, Scope);
            return Convert.ToBase64String(cipherBytes);
        }
        catch (PlatformNotSupportedException)
        {
            // Fallback for non-Windows: Base64 (reversible, NOT secure)
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }
    }

    /// <summary>Decrypt a protected secret. Returns the original plain text.</summary>
    public static string Unprotect(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            var cipherBytes = Convert.FromBase64String(cipherText);
            var plainBytes = ProtectedData.Unprotect(cipherBytes, null, Scope);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (PlatformNotSupportedException)
        {
            // Fallback for non-Windows
            return Encoding.UTF8.GetString(Convert.FromBase64String(cipherText));
        }
        catch (FormatException)
        {
            // Data was stored in plain text before encryption was introduced
            return cipherText;
        }
        catch (CryptographicException)
        {
            // Data was encrypted under a different user context — cannot decrypt.
            // Return empty to avoid leaking ciphertext into UI.
            return string.Empty;
        }
    }

    /// <summary>
    /// Convenience: decrypts all sensitive fields in an <see cref="AuthCredential"/> in-place
    /// and returns a copy with plain-text values for use in HTTP clients.
    /// </summary>
    public static AuthCredentialPlain Decrypt(AuthCredential credential)
    {
        return new AuthCredentialPlain
        {
            Scheme = credential.Scheme,
            Username = credential.Username,
            Password = Unprotect(credential.EncryptedPassword),
            Token = Unprotect(credential.EncryptedToken),
            LoginUrl = credential.LoginUrl,
            ApiKeyHeaderName = credential.ApiKeyHeaderName,
            ApiKey = Unprotect(credential.EncryptedApiKey),
        };
    }

    /// <summary>
    /// Convenience: encrypts plain-text values back into an <see cref="AuthCredential"/>.
    /// </summary>
    public static AuthCredential Encrypt(AuthCredentialPlain plain)
    {
        return new AuthCredential
        {
            Scheme = plain.Scheme,
            Username = plain.Username,
            EncryptedPassword = Protect(plain.Password),
            EncryptedToken = Protect(plain.Token),
            LoginUrl = plain.LoginUrl,
            ApiKeyHeaderName = plain.ApiKeyHeaderName,
            EncryptedApiKey = Protect(plain.ApiKey),
        };
    }
}

/// <summary>
/// Plain-text version of <see cref="AuthCredential"/> for use at runtime.
/// Never persisted to disk.
/// </summary>
public class AuthCredentialPlain
{
    public AuthScheme Scheme { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string LoginUrl { get; set; } = string.Empty;
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";
    public string ApiKey { get; set; } = string.Empty;
}
