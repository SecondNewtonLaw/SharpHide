using System.IO;
using System.Security.Cryptography;

namespace SharpHide.Core.Payloads;

// Verifies the assembly body against its HMAC trailer.
public static class Integrity {
    public const int TrailerLength = 32;
    public static string AuthenticationKey = string.Empty;
    public static bool Strict;
    public static bool Diagnostics;

    public static bool IsIntact() {
        try {
            var location = typeof(Integrity).Assembly.Location;
            if (string.IsNullOrEmpty(location))
                return !Strict;

            var bytes = File.ReadAllBytes(location);
            if (bytes.Length <= TrailerLength)
                return false;

            var body = bytes.AsSpan(0, bytes.Length - TrailerLength);
            var expected = bytes.AsSpan(bytes.Length - TrailerLength);
            var key = Convert.FromBase64String(AuthenticationKey);
            return CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(key, body), expected);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or FormatException or ArgumentException) {
            return !Strict;
        }
    }

    public static void Verify() {
        if (!IsIntact()) {
            if (Diagnostics)
                throw new BadImageFormatException();
            Environment.FailFast(null);
        }
    }

    public static byte[] Trailer(byte[] fileBytes, byte[] key) => HMACSHA256.HashData(key, fileBytes);
}
