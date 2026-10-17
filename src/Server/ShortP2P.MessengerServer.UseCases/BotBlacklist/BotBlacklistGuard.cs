namespace ShortP2P.MessengerServer.UseCases.BotBlacklist;

/// <summary>Rejects blacklisted bots with Forbidden (403) and records access log + warning.</summary>
public sealed class BotBlacklistGuard(
    BotBlacklistService blacklist,
    BotAccessLogService accessLog)
{
    public async Task EnsureAllowedAsync(
        string? networkId,
        string operation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(networkId))
            return;

        var id = networkId.Trim();
        if (!await blacklist.ContainsAsync(id, cancellationToken).ConfigureAwait(false))
            return;

        const string detail = "Bot is not allowed.";
        await accessLog
            .WriteAsync(operation, id, botName: null, BotAccessLogService.OutcomeForbidden, detail, cancellationToken)
            .ConfigureAwait(false);

        throw UseCaseException.Forbidden(detail);
    }
}
