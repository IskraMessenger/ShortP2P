using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Api.Persistence.InMemory;

public sealed class InMemoryBlobChunkRepository(InMemoryMessengerStore store) : IBlobChunkRepository
{
    public Task<BlobChunk?> FindAsync(string blobId, int chunkIndex, CancellationToken cancellationToken = default)
    {
        store.BlobChunks.TryGetValue((blobId, chunkIndex), out var chunk);
        return Task.FromResult(chunk);
    }

    public Task<IReadOnlyList<BlobChunk>> ListByBlobIdAsync(string blobId, CancellationToken cancellationToken = default)
    {
        var list = store.BlobChunks.Values
            .Where(c => string.Equals(c.BlobId, blobId, StringComparison.Ordinal))
            .OrderBy(c => c.ChunkIndex)
            .ToArray();
        return Task.FromResult<IReadOnlyList<BlobChunk>>(list);
    }

    public Task<bool> HasAnyAsync(string blobId, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.BlobChunks.Keys.Any(k => string.Equals(k.BlobId, blobId, StringComparison.Ordinal)));

    public Task AddAsync(BlobChunk chunk, CancellationToken cancellationToken = default)
    {
        store.BlobChunks[(chunk.BlobId, chunk.ChunkIndex)] = chunk;
        return Task.CompletedTask;
    }

    public Task RemoveByBlobIdAsync(string blobId, CancellationToken cancellationToken = default)
    {
        foreach (var key in store.BlobChunks.Keys.Where(k => string.Equals(k.BlobId, blobId, StringComparison.Ordinal)).ToArray())
            store.BlobChunks.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        foreach (var key in store.BlobChunks
                     .Where(p => p.Value.CreatedUtc < cutoffUtc)
                     .Select(p => p.Key)
                     .ToArray())
            store.BlobChunks.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
