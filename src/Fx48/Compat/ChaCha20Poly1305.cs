#if NETFRAMEWORK
using Org.BouncyCastle.Crypto.Parameters;
using BcChaCha20Poly1305 = Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305;

namespace System.Security.Cryptography;

/// <summary>ChaCha20-Poly1305 polyfill for net472 (RFC 8439; same envelope as .NET ChaCha20Poly1305).</summary>
internal sealed class ChaCha20Poly1305 : IDisposable
{
    public const int NonceSize = 12;
    public const int TagSize = 16;
    private const int KeySize = 32;

    private readonly byte[] _key;
    private bool _disposed;

    public ChaCha20Poly1305(ReadOnlySpan<byte> key)
    {
        if (key.Length != KeySize)
            throw new global::System.ArgumentException($"ChaCha20-Poly1305 key must be {KeySize} bytes.", nameof(key));
        _key = key.ToArray();
    }

    public void Encrypt(
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> plaintext,
        Span<byte> ciphertext,
        Span<byte> tag)
    {
        ThrowIfDisposed();
        if (nonce.Length != NonceSize)
            throw new global::System.ArgumentException($"Nonce must be {NonceSize} bytes.", nameof(nonce));
        if (ciphertext.Length != plaintext.Length)
            throw new global::System.ArgumentException("Ciphertext length must match plaintext.");
        if (tag.Length != TagSize)
            throw new global::System.ArgumentException("Tag length mismatch.", nameof(tag));

        var output = Process(true, nonce, plaintext);
        if (output.Length != plaintext.Length + TagSize)
            throw new CryptographicException("Unexpected ChaCha20-Poly1305 encrypt output length.");
        output.AsSpan(0, plaintext.Length).CopyTo(ciphertext);
        output.AsSpan(plaintext.Length, TagSize).CopyTo(tag);
    }

    public void Decrypt(
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> tag,
        Span<byte> plaintext)
    {
        ThrowIfDisposed();
        if (nonce.Length != NonceSize)
            throw new global::System.ArgumentException($"Nonce must be {NonceSize} bytes.", nameof(nonce));
        if (plaintext.Length != ciphertext.Length)
            throw new global::System.ArgumentException("Plaintext length must match ciphertext.");
        if (tag.Length != TagSize)
            throw new global::System.ArgumentException("Tag length mismatch.", nameof(tag));

        var packed = new byte[ciphertext.Length + tag.Length];
        ciphertext.CopyTo(packed);
        tag.CopyTo(packed.AsSpan(ciphertext.Length));
        byte[] output;
        try
        {
            output = Process(false, nonce, packed);
        }
        catch (Org.BouncyCastle.Crypto.InvalidCipherTextException ex)
        {
            // Match .NET: authentication failure surfaces as CryptographicException.
            throw new CryptographicException("The computed authentication tag did not match the input.", ex);
        }

        if (output.Length != plaintext.Length)
            throw new CryptographicException("Unexpected ChaCha20-Poly1305 decrypt output length.");
        output.CopyTo(plaintext);
    }

    private byte[] Process(bool forEncryption, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> input)
    {
        var cipher = new BcChaCha20Poly1305();
        cipher.Init(forEncryption, new AeadParameters(new KeyParameter(_key), TagSize * 8, nonce.ToArray()));
        var output = new byte[cipher.GetOutputSize(input.Length)];
        var len = cipher.ProcessBytes(input.ToArray(), 0, input.Length, output, 0);
        len += cipher.DoFinal(output, len);
        if (len == output.Length)
            return output;
        var trimmed = new byte[len];
        Buffer.BlockCopy(output, 0, trimmed, 0, len);
        return trimmed;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ChaCha20Poly1305));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        Array.Clear(_key, 0, _key.Length);
        _disposed = true;
    }
}
#endif
