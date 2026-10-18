using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.Blobs;

public sealed class DeleteBlobUseCase(IBlobRepository blobs)
{
    public async Task ExecuteAsync(DeleteBlobCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var blobId = command.BlobId.Trim();
        var caller = command.CallerNetworkId.Trim();
        if (caller.Length == 0)
            throw UseCaseException.Unauthorized("Missing network id.");

        var blob = await blobs.FindByIdAsync(blobId, cancellationToken).ConfigureAwait(false);
        if (blob == null)
            return;

        if (!string.Equals(blob.SrcNetworkId, caller, StringComparison.Ordinal) &&
            !string.Equals(blob.TgtNetworkId, caller, StringComparison.Ordinal))
            throw UseCaseException.Unauthorized("Not allowed to delete this blob.");

        await blobs.RemoveByIdAsync(blobId, cancellationToken).ConfigureAwait(false);
    }
}
