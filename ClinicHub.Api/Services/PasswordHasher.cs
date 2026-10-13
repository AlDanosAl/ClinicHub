using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace ClinicHub.Api.Services;

/// <summary>PBKDF2-хеширование паролей со своей солью (без Identity).</summary>
public static class PasswordHasher
{
    public const int Iterations = 100_000;

    public static string Hash(string password, string salt)
    {
        return Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: Convert.FromBase64String(salt),
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: 32));
    }

    public static string GenerateSalt()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(bytes);
    }

    public static bool Verify(string password, string salt, string expectedHash)
    {
        var actual = Hash(password, salt);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(actual),
            Convert.FromBase64String(expectedHash));
    }
}