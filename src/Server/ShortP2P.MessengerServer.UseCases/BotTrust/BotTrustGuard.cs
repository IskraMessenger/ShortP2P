using ShortP2P.MessengerServer.UseCases.BotAccess;

namespace ShortP2P.MessengerServer.UseCases.BotTrust;

/// <summary>Rejects low-trust bots (rating &lt; 0.5) with Forbidden (403) and records access log.</summary>
public sealed class BotTrustGuard(
    BotTrustEngine trust,
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
        if (!await trust.IsBannedAsync(id, cancellationToken).ConfigureAwait(false))
            return;

        const string detail = "Bot trust rating is too low.";
        await accessLog
            .WriteAsync(operation, id, botName: null, BotAccessLogService.OutcomeForbidden, detail, cancellationToken)
            .ConfigureAwait(false);

        throw UseCaseException.Forbidden(detail);
    }
}
