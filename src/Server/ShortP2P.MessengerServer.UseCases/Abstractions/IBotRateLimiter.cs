namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>Per-bot request rate limiter for send_messages (1 HTTP request = 1 unit).</summary>
public interface IBotRateLimiter
{
    /// <summary>Count a send_messages attempt for RPM metrics (allowed or denied).</summary>
    void NoteRequest();

    /// <summary>
    /// Atomically take and reset the request count accumulated since the previous take.
    /// </summary>
    long TakeRequestCount();

    /// <summary>Current per-bot RPS limit from the hardware formula.</summary>
    Task<double> GetRpsLimitAsync(CancellationToken cancellationToken = default);

    Task<BotRateLimitDecision> TryAcquireAsync(
        string botNetworkId,
        CancellationToken cancellationToken = default);
}
