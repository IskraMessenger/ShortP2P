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
        var caller = command.CallerNetworkId.Trim();
        var botNetworkId = command.BotNetworkId.Trim();
        var messageId = command.MessageId.Trim();
        var correlationId = string.IsNullOrWhiteSpace(command.CorrelationId)
            ? null
            : command.CorrelationId.Trim();
        var ciphertext = command.Message.Trim();

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
