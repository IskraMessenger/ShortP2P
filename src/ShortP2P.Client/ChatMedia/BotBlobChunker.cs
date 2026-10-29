using System.Security.Cryptography;
using ShortP2P.MessengerServer.Contracts;

namespace ShortP2P.Client.ChatMedia;

public static class BotBlobChunker
{
    public static byte[][] Split(ReadOnlySpan<byte> ciphertext)
    {
        if (ciphertext.Length == 0)
            throw new ArgumentException("Ciphertext is empty.", nameof(ciphertext));

        var size = BlobLimits.ChunkSizeBytes;
        var count = (ciphertext.Length + size - 1) / size;
        var chunks = new byte[count][];
        for (var i = 0; i < count; i++)
        {
            var offset = i * size;
            var length = Math.Min(size, ciphertext.Length - offset);
            chunks[i] = ciphertext.Slice(offset, length).ToArray();
        }

        return chunks;
    }

    public static byte[] Join(IReadOnlyList<byte[]> chunks)
    {
        if (chunks == null)
            throw new ArgumentNullException(nameof(chunks));
        var length = 0;
        foreach (var chunk in chunks)
            length += chunk?.Length ?? 0;

        var result = new byte[length];
        var offset = 0;
        foreach (var chunk in chunks)
        {
            if (chunk == null || chunk.Length == 0)
                continue;
            chunk.CopyTo(result, offset);
            offset += chunk.Length;
        }

        return result;
    }

    public static string Sha256Hex(ReadOnlySpan<byte> ciphertext)
    {
#if NETCOREAPP
        var hash = SHA256.HashData(ciphertext);
        return Convert.ToHexString(hash).ToLowerInvariant();
#else
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(ciphertext.ToArray());
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
#endif
    }
}
