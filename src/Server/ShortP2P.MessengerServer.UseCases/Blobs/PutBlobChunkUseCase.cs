using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.Blobs;

public sealed class PutBlobChunkUseCase(
    IBlobChunkRepository chunks,
    IClock clock,
    IBotAccountRepository bots,
    IServerDiskSpace disk)
{
    public async Task ExecuteAsync(PutBlobChunkCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var blobId = command.BlobId.Trim();
        var src = command.SrcNetworkId.Trim();
        var tgt = command.TgtNetworkId.Trim();
        var ciphertext = command.Ciphertext ?? [];
        var index = command.ChunkIndex;
        var totalChunks = command.TotalChunks;
        var totalBytes = command.TotalBytes;

        if (src.Length == 0)
            throw UseCaseException.Unauthorized("Missing network id.");

        var expected = ExpectedLength(index, totalChunks, totalBytes);
        if (ciphertext.Length != expected)
        {
            throw UseCaseException.Validation(
                $"Chunk {index} must be {expected} bytes.");
        }

        var existingForBlob = await chunks.ListByBlobIdAsync(blobId, cancellationToken).ConfigureAwait(false);
        if (existingForBlob.Count > 0)
        {
            var head = existingForBlob[0];
            if (!string.Equals(head.SrcNetworkId, src, StringComparison.Ordinal) ||
                !string.Equals(head.TgtNetworkId, tgt, StringComparison.Ordinal) ||
                head.TotalChunks != totalChunks ||
                head.TotalBytes != totalBytes)
            {
                throw UseCaseException.Conflict("A blob with this id already exists.");
            }
        }

        var existing = await chunks.FindAsync(blobId, index, cancellationToken).ConfigureAwait(false);
        if (existing != null)
        {
            if (existing.Ciphertext.AsSpan().SequenceEqual(ciphertext))
                return;

            throw UseCaseException.Conflict("A chunk with this index already exists.");
        }

        var sender = await bots.FindByNetworkIdAsync(src, cancellationToken).ConfigureAwait(false);
        if (sender != null)
        {
            var reserved = await chunks
                .SumIncompleteUploadBytesAsync(blobId, cancellationToken)
                .ConfigureAwait(false);
            if (!BotUploadDiskBudget.Fits(disk.AvailableFreeBytes, reserved, totalBytes))
            {
                throw UseCaseException.InsufficientStorage(
                    "Not enough free space to accept this bot upload.");
            }
        }

        await chunks.AddAsync(
            new BlobChunk
            {
                BlobId = blobId,
                ChunkIndex = index,
                SrcNetworkId = src,
                TgtNetworkId = tgt,
                TotalChunks = totalChunks,
                TotalBytes = totalBytes,
                Ciphertext = ciphertext,
                CreatedUtc = clock.UtcNow
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Byte length of chunk <paramref name="index"/> given the declared totals.</summary>
    public static int ExpectedLength(int index, int totalChunks, long totalBytes)
    {
        if (totalChunks < 1 || index < 0 || index >= totalChunks)
            throw UseCaseException.Validation("Chunk index is outside the declared range.");
        if (totalBytes < 1 || totalBytes > BlobLimits.MaxCiphertextBytes)
            throw UseCaseException.Validation("totalBytes is outside the allowed range.");

        var full = BlobLimits.ChunkSizeBytes;
        var tail = totalBytes - (long)(totalChunks - 1) * full;
        if (tail < 1 || tail > full)
            throw UseCaseException.Validation("totalBytes does not match the chunk size.");

        return index == totalChunks - 1 ? (int)tail : full;
    }
}
