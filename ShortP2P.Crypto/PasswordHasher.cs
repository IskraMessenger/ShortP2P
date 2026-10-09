using System.Security.Cryptography;

namespace ShortP2P.Crypto;

public static class PasswordHasher
{
    /// <summary>Login password salt size (TRL-10: fixed 32 bytes).</summary>
    public const int SaltSize = 32;

    public const int KeySize = 32;

    public const int Iterations = 120_000;

    public static (string SaltBase64, string HashBase64) Hash(string password)
    {
        var salt = GetRandomBytes(SaltSize);
        return Hash(password, salt);
    }

    /// <summary>Hash with a caller-supplied salt (must be <see cref="SaltSize"/> bytes).</summary>
    public static (string SaltBase64, string HashBase64) Hash(string password, byte[] salt)
    {
        Require.NotNull(salt);
        if (salt.Length != SaltSize)
            throw new ArgumentException($"Salt must be exactly {SaltSize} bytes.", nameof(salt));

        var hash = Pbkdf2(password, salt, KeySize, Iterations);
        return (Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public static bool Verify(string password, string saltBase64, string hashBase64)
    {
        var salt = Convert.FromBase64String(saltBase64);
        var expected = Convert.FromBase64String(hashBase64);
        var actual = Pbkdf2(password, salt, expected.Length, Iterations);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    /// <summary>Decoded salt length in bytes (0 if invalid Base64).</summary>
    public static int GetSaltLength(string saltBase64)
    {
        if (string.IsNullOrWhiteSpace(saltBase64))
            return 0;
        try
        {
            return Convert.FromBase64String(saltBase64).Length;
        }
        catch (FormatException)
        {
            return 0;
        }
    }

    public static byte[] GetRandomSalt() => GetRandomBytes(SaltSize);

    private static byte[] GetRandomBytes(int count)
    {
#if NET6_0_OR_GREATER
        return RandomNumberGenerator.GetBytes(count);
#else
        var bytes = new byte[count];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
#endif
    }

    private static byte[] Pbkdf2(string password, byte[] salt, int keySize, int iterations)
    {
#if NET6_0_OR_GREATER
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, keySize);
#else
        using var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return kdf.GetBytes(keySize);
#endif
    }
}
