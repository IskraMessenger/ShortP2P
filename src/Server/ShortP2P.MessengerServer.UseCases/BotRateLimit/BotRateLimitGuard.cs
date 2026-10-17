using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotRateLimit;

/// <summary>Rejects bots that exceed send_messages RPS / cooldown with TooManyRequests (429).</summary>
public sealed class BotRateLimitGuard(
    IBotRateLimiter limiter,
    IOptions<BotRateLimitOptions> options)
{
    public async Task EnsureAllowedAsync(
        string botNetworkId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(botNetworkId))
            return;

        limiter.NoteRequest();

        if (!options.Value.Enabled)
            return;

        var decision = await limiter
            .TryAcquireAsync(botNetworkId.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (decision.IsAllowed)
            return;

        throw UseCaseException.TooManyRequests(
            "Bot rate limit exceeded.",
            decision.RetryAfterSeconds);
    }
}
