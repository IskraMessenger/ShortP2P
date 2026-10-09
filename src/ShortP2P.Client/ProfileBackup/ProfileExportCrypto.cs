using System.Security.Cryptography;
using ShortP2P.Crypto;

namespace ShortP2P.Client.ProfileBackup;

/// <summary>
/// Master-password KDF + ChaCha20-Poly1305 for <c>.tlp</c> profile backup files (TRL-10).
/// </summary>
public static class ProfileExportCrypto
{
    public const int KdfIdPbkdf2Sha256 = 1;
    public const int ExportKdfIterations = 600_000;
    public const int KeySize = 32;
    public const int NonceSize = 12;
    public const int TagSize = 16;
    public const int KdfSaltSize = 32;

    public static byte[] DeriveKey(string masterPassword, ReadOnlySpan<byte> kdfSalt, int iterations)
    {
        Require.NotNull(masterPassword);
        if (kdfSalt.Length != KdfSaltSize)
            throw new ArgumentException($"KDF salt must be {KdfSaltSize} bytes.", nameof(kdfSalt));
        if (iterations < 1)
            throw new ArgumentOutOfRangeException(nameof(iterations));

        return Pbkdf2(masterPassword, kdfSalt.ToArray(), iterations, KeySize);
    }

    public static byte[] CreateKdfSalt() => GetRandomBytes(KdfSaltSize);

    public static byte[] CreateNonce() => GetRandomBytes(NonceSize);

    private static byte[] Pbkdf2(string password, byte[] salt, int iterations, int keySize)
    {
#if NET6_0_OR_GREATER
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, keySize);
#else
        using var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return kdf.GetBytes(keySize);
#endif
    }

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

    /// <summary>
    /// Encrypts plaintext; returns ciphertext||tag (tag last <see cref="TagSize"/> bytes).
    /// </summary>
    public static byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes.", nameof(key));
        if (nonce.Length != NonceSize)
            throw new ArgumentException($"Nonce must be {NonceSize} bytes.", nameof(nonce));

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        using var aead = new ChaCha20Poly1305(key);
        aead.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[ciphertext.Length + TagSize];
        Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, ciphertext.Length, TagSize);
        return result;
    }

    /// <summary>
    /// Decrypts ciphertext||tag. Throws <see cref="CryptographicException"/> on auth failure.
    /// </summary>
    public static byte[] Decrypt(ReadOnlySpan<byte> ciphertextAndTag, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes.", nameof(key));
        if (nonce.Length != NonceSize)
            throw new ArgumentException($"Nonce must be {NonceSize} bytes.", nameof(nonce));
        if (ciphertextAndTag.Length < TagSize)
            throw new CryptographicException("Ciphertext too short.");

        var ctLen = ciphertextAndTag.Length - TagSize;
        var ciphertext = ciphertextAndTag[..ctLen];
        var tag = ciphertextAndTag[ctLen..];
        var plaintext = new byte[ctLen];
        using var aead = new ChaCha20Poly1305(key);
        aead.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }
}
