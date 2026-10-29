using Microsoft.EntityFrameworkCore;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Persistence.Psql.Entities;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Persistence.Psql.Repositories;

public sealed class PostgresBlobChunkRepository(MessengerDbContext db) : IBlobChunkRepository
{
    public async Task<BlobChunk?> FindAsync(string blobId, int chunkIndex, CancellationToken cancellationToken = default)
    {
        var row = await db.BlobChunks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BlobId == blobId && x.ChunkIndex == chunkIndex, cancellationToken)
            .ConfigureAwait(false);
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<BlobChunk>> ListByBlobIdAsync(
        string blobId,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.BlobChunks.AsNoTracking()
            .Where(x => x.BlobId == blobId)
            .OrderBy(x => x.ChunkIndex)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(x => x.ToDomain()).ToArray();
    }

    public Task<bool> HasAnyAsync(string blobId, CancellationToken cancellationToken = default) =>
        db.BlobChunks.AsNoTracking().AnyAsync(x => x.BlobId == blobId, cancellationToken);

    public async Task<long> SumIncompleteUploadBytesAsync(
        string excludeBlobId,
        CancellationToken cancellationToken = default)
    {
        var groups = await db.BlobChunks.AsNoTracking()
            .Where(x => x.BlobId != excludeBlobId)
            .GroupBy(x => x.BlobId)
            .Select(g => new
            {
                TotalBytes = g.Max(x => x.TotalBytes),
                Declared = g.Max(x => x.TotalChunks),
                Stored = g.Count()
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        long sum = 0;
        foreach (var group in groups)
        {
            if (group.Stored < group.Declared)
                sum += group.TotalBytes;
        }

        return sum;
    }

    public async Task AddAsync(BlobChunk chunk, CancellationToken cancellationToken = default)
    {
        db.BlobChunks.Add(BlobChunkRecord.FromDomain(chunk));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveByBlobIdAsync(string blobId, CancellationToken cancellationToken = default)
    {
        await db.BlobChunks
            .Where(x => x.BlobId == blobId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        await db.BlobChunks
            .Where(x => x.CreatedUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
