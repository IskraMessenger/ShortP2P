using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotClient;

/// <summary>Long-poll of encrypted bot → client messages. Does not delete them; a receipt does.</summary>
public sealed class WaitForBotMessagesUseCase(
    IBotToClientMessageRepository messages,
    IBotToClientWaitService wait,
    IOptions<MessengerInboxOptions> inboxOptions,
    IClock clock)
{
    public async Task<IReadOnlyList<BotToClientMessage>> ExecuteAsync(
        WaitForBotMessagesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.CallerNetworkId))
            throw UseCaseException.Validation("NetworkId is required.");

        var caller = query.CallerNetworkId.Trim();
        var opts = inboxOptions.Value;
        var timeoutSeconds = query.TimeoutSeconds ?? opts.MaxPollTimeoutSeconds;
        if (timeoutSeconds < 1 || timeoutSeconds > opts.MaxPollTimeoutSeconds)
            throw UseCaseException.Validation($"timeoutSeconds must be between 1 and {opts.MaxPollTimeoutSeconds}.");

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
        return list
            .Where(m => m.CreatedUtc >= cutoff)
            .OrderBy(m => m.CreatedUtc)
            .ToArray();
    }
}
