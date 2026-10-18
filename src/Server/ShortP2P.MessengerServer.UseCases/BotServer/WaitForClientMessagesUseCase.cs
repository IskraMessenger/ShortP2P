using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

/// <summary>
/// Long-poll of encrypted client → bot messages. Returned messages are removed from the queue.
/// </summary>
public sealed class WaitForClientMessagesUseCase(
    IBotAccountRepository accounts,
    IClientToBotMessageRepository messages,
    IClientToBotWaitService wait,
    IOptions<MessengerInboxOptions> inboxOptions,
    IPasswordHasher passwordHasher,
    IClock clock,
    BotBlacklistGuard blacklistGuard)
{
    public async Task<IReadOnlyList<ClientToBotMessage>> ExecuteAsync(
        WaitForClientMessagesQuery query,
        CancellationToken cancellationToken = default)
    {
        await blacklistGuard
            .EnsureAllowedAsync(query.BotNetworkId, BotAccessOperations.WaitForIncomeMessages, cancellationToken)
            .ConfigureAwait(false);

        var account = await BotAuthentication.AuthenticateAsync(
            accounts,
            passwordHasher,
            query.BotNetworkId,
            query.BotKey,
            botName: null,
            requireBotName: false,
            cancellationToken).ConfigureAwait(false);

        var opts = inboxOptions.Value;
        var timeoutSeconds = query.TimeoutSeconds ?? opts.MaxPollTimeoutSeconds;

        var taken = await messages.TakeForBotAsync(account.NetworkId, cancellationToken).ConfigureAwait(false);
        var pending = FilterExpired(taken);
        if (pending.Count > 0)
            return pending;

        await wait
            .WaitAsync(account.NetworkId, TimeSpan.FromSeconds(timeoutSeconds), cancellationToken)
            .ConfigureAwait(false);

        taken = await messages.TakeForBotAsync(account.NetworkId, cancellationToken).ConfigureAwait(false);
        return FilterExpired(taken);
    }

    private IReadOnlyList<ClientToBotMessage> FilterExpired(IReadOnlyList<ClientToBotMessage> taken)
    {
        var cutoff = clock.UtcNow - inboxOptions.Value.MessageRetention;
        return taken.Where(m => m.CreatedUtc >= cutoff).ToArray();
    }
}
