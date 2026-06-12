using System.Security.Cryptography;
using System.Text;

namespace Rai.Shared.Hmac;

/// <summary>Provides HMAC-SHA256 signing and constant-time verification for webhook payloads.</summary>
public static class HmacSigner
{
    private const string HeaderName = "X-Hub-Signature-256";

    /// <summary>The HTTP header name used to transmit the HMAC signature.</summary>
    public static string SignatureHeader => HeaderName;

    /// <summary>Computes an HMAC-SHA256 hex digest of <paramref name="payload"/> using <paramref name="secret"/>.</summary>
    public static string Sign(string payload, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="signature"/> matches the HMAC-SHA256 of
    /// <paramref name="payload"/>. Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    public static bool Verify(string payload, string secret, string signature)
    {
        var expected = Sign(payload, secret);
        // Constant-time compare (same length required; pad if needed to avoid length leak)
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(signature.PadRight(expected.Length));
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes)
               && expected.Length == signature.Length;
    }
}
