using System.Security.Cryptography;

namespace tric.web.Services;

/// <summary>
/// Creates the RSA key material required for the session registration.
/// </summary>
public static class KeyService
{
    /// <summary>
    /// Generates a new 2048 bit RSA key and returns only the public key as PEM (PKCS#1, "BEGIN RSA PUBLIC KEY").
    /// The private key is never needed by the API and is discarded.
    /// </summary>
    public static string CreatePublicKeyPem()
    {
        using var rsa = RSA.Create(2048);
        return rsa.ExportRSAPublicKeyPem();
    }
}
