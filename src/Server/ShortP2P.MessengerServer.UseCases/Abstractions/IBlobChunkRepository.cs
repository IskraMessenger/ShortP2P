using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.Abstractions;

public interface IBlobChunkRepository
{
    Task<BlobChunk?> FindAsync(string blobId, int chunkIndex, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlobChunk>> ListByBlobIdAsync(string blobId, CancellationToken cancellationToken = default);

    Task<bool> HasAnyAsync(string blobId, CancellationToken cancellationToken = default);

    Task AddAsync(BlobChunk chunk, CancellationToken cancellationToken = default);

    Task RemoveByBlobIdAsync(string blobId, CancellationToken cancellationToken = default);

    Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
