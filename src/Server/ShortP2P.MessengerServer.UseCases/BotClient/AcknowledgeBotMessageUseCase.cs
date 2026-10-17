using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotClient;

/// <summary>Client delivery receipt: drops the stored bot → client message.</summary>
public sealed class AcknowledgeBotMessageUseCase(IBotToClientMessageRepository messages)
{
    public async Task ExecuteAsync(
        AcknowledgeBotMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.CallerNetworkId) || string.IsNullOrWhiteSpace(command.MessageId))
            throw UseCaseException.Validation("NetworkId and messageId are required.");

        var caller = command.CallerNetworkId.Trim();
        var messageId = command.MessageId.Trim();
        var message = await messages.FindByIdAsync(messageId, cancellationToken).ConfigureAwait(false);
        if (message is null)
            throw UseCaseException.NotFound("Message not found.");

        if (!string.Equals(message.ClientNetworkId, caller, StringComparison.Ordinal))
            throw UseCaseException.Unauthorized("Only the message recipient can submit a delivery receipt.");

        await messages.RemoveAsync(messageId, cancellationToken).ConfigureAwait(false);
    }
}
