using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.Blobs;

public sealed class GetBlobChunkUseCase(IBlobChunkRepository chunks)
{
    public async Task<BlobChunk> ExecuteAsync(GetBlobChunkQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var caller = query.CallerNetworkId.Trim();
        if (caller.Length == 0)
            throw UseCaseException.Unauthorized("Missing network id.");

        var chunk = await chunks
            .FindAsync(query.BlobId.Trim(), query.ChunkIndex, cancellationToken)
            .ConfigureAwait(false);
        if (chunk == null)
            throw UseCaseException.NotFound("Blob chunk not found.");

        if (!IsParty(chunk, caller))
            throw UseCaseException.Unauthorized("Not allowed to download this blob.");

        return chunk;
    }

    internal static bool IsParty(BlobChunk chunk, string caller) =>
        string.Equals(chunk.SrcNetworkId, caller, StringComparison.Ordinal) ||
        string.Equals(chunk.TgtNetworkId, caller, StringComparison.Ordinal);
}
