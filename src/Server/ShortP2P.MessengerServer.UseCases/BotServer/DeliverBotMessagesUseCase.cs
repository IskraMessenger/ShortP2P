using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;
using ShortP2P.MessengerServer.UseCases.BotRateLimit;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

/// <summary>
/// Stores encrypted bot → client messages until the client receipt. Does not delete them on send.
/// </summary>
public sealed class DeliverBotMessagesUseCase(
    IBotAccountRepository accounts,
    IBotToClientMessageRepository messages,
    IBotToClientWaitService wait,
    IPasswordHasher passwordHasher,
    IClock clock,
    BotBlacklistGuard blacklistGuard,
    BotRateLimitGuard rateLimitGuard)
{
    public async Task ExecuteAsync(
        DeliverBotMessagesCommand command,
        CancellationToken cancellationToken = default)
    {
        await blacklistGuard
            .EnsureAllowedAsync(command.BotNetworkId, BotAccessOperations.SendMessages, cancellationToken)
            .ConfigureAwait(false);

        var account = await BotAuthentication.AuthenticateAsync(
            accounts,
            passwordHasher,
            command.BotNetworkId,
            command.BotKey,
            botName: null,
            requireBotName: false,
            cancellationToken).ConfigureAwait(false);

        await rateLimitGuard
            .EnsureAllowedAsync(account.NetworkId, cancellationToken)
            .ConfigureAwait(false);

        var pending = new List<BotToClientMessage>(command.Messages.Count);
        foreach (var item in command.Messages)
        {
            var correlation = string.IsNullOrWhiteSpace(item.CorrelationId)
                ? null
                : item.CorrelationId.Trim();

            pending.Add(new BotToClientMessage
            {
                MessageId = Guid.NewGuid().ToString("N"),
                BotNetworkId = account.NetworkId,
                ClientNetworkId = item.ClientNetworkId.Trim(),
                CreatedUtc = clock.UtcNow,
                EncryptedMessageBase64 = item.EncryptedMessageBase64.Trim(),
                CorrelationId = correlation
            });
        }

        foreach (var message in pending)
        {
            await messages.AddAsync(message, cancellationToken).ConfigureAwait(false);
            wait.Notify(message.ClientNetworkId);
        }
    }
}
