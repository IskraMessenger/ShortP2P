using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.Blobs;

public sealed class GetBlobChunkManifestUseCase(IBlobChunkRepository chunks)
{
    public async Task<BlobChunkManifest> ExecuteAsync(
        GetBlobChunkManifestQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var caller = query.CallerNetworkId.Trim();
        if (caller.Length == 0)
            throw UseCaseException.Unauthorized("Missing network id.");

        var blobId = query.BlobId.Trim();
        var stored = await chunks.ListByBlobIdAsync(blobId, cancellationToken).ConfigureAwait(false);
        if (stored.Count == 0)
            throw UseCaseException.NotFound("Blob not found.");

        var head = stored[0];
        if (!GetBlobChunkUseCase.IsParty(head, caller))
            throw UseCaseException.Unauthorized("Not allowed to download this blob.");

        var indexes = stored.Select(c => c.ChunkIndex).Distinct().OrderBy(i => i).ToArray();
        return new BlobChunkManifest(blobId, head.TotalChunks, head.TotalBytes, indexes);
    }
}
