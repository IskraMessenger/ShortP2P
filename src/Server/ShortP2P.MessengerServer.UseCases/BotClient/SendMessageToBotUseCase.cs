using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;

namespace ShortP2P.MessengerServer.UseCases.BotClient;

/// <summary>Stores one encrypted client → bot message and wakes the bot long-poll.</summary>
public sealed class SendMessageToBotUseCase(
    IBotAccountRepository bots,
    IClientToBotMessageRepository messages,
    IClientToBotWaitService wait,
    BotBlacklistService blacklist)
{
    public async Task ExecuteAsync(SendMessageToBotCommand command, CancellationToken cancellationToken = default)
    {
        if (command.DateTime == default)
            throw UseCaseException.Validation("MessageId, networkId, dateTime and message are required.");

        var caller = InputGuard.NetworkId(command.CallerNetworkId, "callerNetworkId");
        var botNetworkId = InputGuard.NetworkId(command.BotNetworkId, "botNetworkId");
        var messageId = InputGuard.MessageId(command.MessageId);
        var correlationId = InputGuard.CorrelationId(command.CorrelationId);
        var ciphertext = InputGuard.CiphertextBase64(
            command.Message, MessageLimits.MaxCiphertextBytes, "message");

        if (await blacklist.ContainsAsync(botNetworkId, cancellationToken).ConfigureAwait(false))
            throw UseCaseException.Forbidden("Bot is not allowed.");

        if (await bots.FindByNetworkIdAsync(botNetworkId, cancellationToken).ConfigureAwait(false) is null)
            throw UseCaseException.NotFound("Bot is not registered.");

        if (await messages.ExistsAsync(messageId, cancellationToken).ConfigureAwait(false))
            return;

        await messages.AddAsync(
            new ClientToBotMessage
            {
                MessageId = messageId,
                ClientNetworkId = caller,
                BotNetworkId = botNetworkId,
                CreatedUtc = DateTime.SpecifyKind(command.DateTime, DateTimeKind.Utc),
                EncryptedMessageBase64 = ciphertext,
                CorrelationId = correlationId
            },
            cancellationToken).ConfigureAwait(false);

        wait.Notify(botNetworkId);
    }
}
