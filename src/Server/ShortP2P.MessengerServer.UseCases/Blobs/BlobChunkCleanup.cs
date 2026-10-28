using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.Blobs;

/// <summary>Deletes ciphertext chunks for a receipt. Missing chunks are a success. A stranger is rejected.</summary>
internal static class BlobChunkCleanup
{
    public static async Task DeleteForCallerAsync(
        IBlobChunkRepository chunks,
        string blobId,
        string caller,
        CancellationToken cancellationToken)
    {
        var stored = await chunks.ListByBlobIdAsync(blobId, cancellationToken).ConfigureAwait(false);
        if (stored.Count == 0)
            return;

        if (!GetBlobChunkUseCase.IsParty(stored[0], caller))
            throw UseCaseException.Unauthorized("Not allowed to delete this blob.");

        await chunks.RemoveByBlobIdAsync(blobId, cancellationToken).ConfigureAwait(false);
    }
}
