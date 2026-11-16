using System.Security.Cryptography;
using LossLens.Core.Abstractions;

namespace LossLens.Infrastructure.Security;

/// <summary>
/// PBKDF2 with HMAC SHA-256 and a random salt per password. The hash is stored as
/// <c>v1.{iterations}.{salt}.{hash}</c> (Base64), so the cost can be raised later without breaking old hashes.
/// </summary>
public sealed class Pbkdf2PasswordHasher(int iterations = Pbkdf2PasswordHasher.DefaultIterations) : IPasswordHasher
{
    public const int DefaultIterations = 600_000;

    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const string Version = "v1";

    private static readonly byte[] DummySalt = new byte[SaltBytes];

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Derive(password, salt, iterations);

        return $"{Version}.{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string? hash)
    {
        ArgumentNullException.ThrowIfNull(password);

        if (hash is null)
        {
            // Same amount of work as a real check, so an unknown user cannot be told apart by timing.
            Derive(password, DummySalt, iterations);
            return false;
        }

        if (!TryParse(hash, out var cost, out var salt, out var expected))
        {
            return false;
        }

        var actual = Derive(password, salt, cost);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string password, byte[] salt, int cost) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, cost, HashAlgorithmName.SHA256, HashBytes);

    private static bool TryParse(string value, out int cost, out byte[] salt, out byte[] hash)
    {
        cost = 0;
        salt = [];
        hash = [];

        var parts = value.Split('.');
        if (parts.Length != 4 || parts[0] != Version || !int.TryParse(parts[1], out cost) || cost <= 0)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            hash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length > 0 && hash.Length == HashBytes;
    }
}
