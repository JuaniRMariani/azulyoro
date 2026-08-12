using System.Security.Cryptography;
using System.Text;

namespace Azulyoro.Infrastructure.Email;

/// <summary>Generates single-use opt-in tokens and stores only their SHA256
/// hash. The raw token travels in the confirm link; the DB keeps the hash.</summary>
public static class TokenHasher
{
    /// <summary>Creates a URL-safe random token (32 bytes, base64url).</summary>
    public static string NewToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    /// <summary>SHA256 hash of the token, hex-encoded, for at-rest storage.</summary>
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }

    /// <summary>Constant-time comparison of a candidate token against a stored hash.</summary>
    public static bool Verify(string token, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        var candidate = Hash(token);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(candidate),
            Encoding.UTF8.GetBytes(storedHash));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
