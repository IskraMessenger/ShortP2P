namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>Per-bot request rate limiter for send_messages (1 HTTP request = 1 unit).</summary>
public interface IBotRateLimiter
{
    Task<BotRateLimitDecision> TryAcquireAsync(
        string botNetworkId,
        CancellationToken cancellationToken = default);
}
