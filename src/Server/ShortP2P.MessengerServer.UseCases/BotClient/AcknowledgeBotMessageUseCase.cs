using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.Blobs;

namespace ShortP2P.MessengerServer.UseCases.BotClient;

/// <summary>Client delivery receipt: drops the stored bot → client message and its ciphertext chunks.</summary>
public sealed class AcknowledgeBotMessageUseCase(
    IBotToClientMessageRepository messages,
    IBlobChunkRepository chunks)
{
    public async Task ExecuteAsync(
        AcknowledgeBotMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        var caller = command.CallerNetworkId.Trim();
        var messageId = command.MessageId.Trim();
        var message = await messages.FindByIdAsync(messageId, cancellationToken).ConfigureAwait(false);
        if (message != null &&
            !string.Equals(message.ClientNetworkId, caller, StringComparison.Ordinal))
        {
            throw UseCaseException.Unauthorized("Only the message recipient can submit a delivery receipt.");
        }

        if (message != null)
            await messages.RemoveAsync(messageId, cancellationToken).ConfigureAwait(false);

        await BlobChunkCleanup.DeleteForCallerAsync(chunks, messageId, caller, cancellationToken)
            .ConfigureAwait(false);
    }
}
