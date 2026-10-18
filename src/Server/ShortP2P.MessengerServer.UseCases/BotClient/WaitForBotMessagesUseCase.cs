using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;

namespace ShortP2P.MessengerServer.UseCases.BotClient;

/// <summary>Long-poll of encrypted bot → client messages. Does not delete them; a receipt does.</summary>
public sealed class WaitForBotMessagesUseCase(
    IBotToClientMessageRepository messages,
    IBotToClientWaitService wait,
    IOptions<MessengerInboxOptions> inboxOptions,
    IClock clock,
    BotBlacklistService blacklist)
{
    public async Task<IReadOnlyList<BotToClientMessage>> ExecuteAsync(
        WaitForBotMessagesQuery query,
        CancellationToken cancellationToken = default)
    {
        var caller = query.CallerNetworkId.Trim();
        var opts = inboxOptions.Value;
        var timeoutSeconds = query.TimeoutSeconds ?? opts.MaxPollTimeoutSeconds;

        var pending = await ListFreshAsync(caller, cancellationToken).ConfigureAwait(false);
        if (pending.Count > 0)
            return pending;

        await wait
            .WaitAsync(caller, TimeSpan.FromSeconds(timeoutSeconds), cancellationToken)
            .ConfigureAwait(false);

        return await ListFreshAsync(caller, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<BotToClientMessage>> ListFreshAsync(
        string caller,
        CancellationToken cancellationToken)
    {
        var cutoff = clock.UtcNow - inboxOptions.Value.MessageRetention;
        var list = await messages.ListForClientAsync(caller, cancellationToken).ConfigureAwait(false);
        var fresh = new List<BotToClientMessage>();
        foreach (var message in list.OrderBy(m => m.CreatedUtc))
        {
            if (message.CreatedUtc < cutoff)
                continue;

            if (await blacklist.ContainsAsync(message.BotNetworkId, cancellationToken).ConfigureAwait(false))
            {
                await messages.RemoveAsync(message.MessageId, cancellationToken).ConfigureAwait(false);
                continue;
            }

            fresh.Add(message);
        }

        return fresh;
    }
}
