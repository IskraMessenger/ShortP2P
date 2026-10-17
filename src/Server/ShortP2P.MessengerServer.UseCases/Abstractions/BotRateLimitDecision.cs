namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>Result of a single bot rate-limit acquire attempt.</summary>
public sealed class BotRateLimitDecision
{
    public required bool IsAllowed { get; init; }

    /// <summary>Suggested Retry-After seconds when denied.</summary>
    public int? RetryAfterSeconds { get; init; }

    public static BotRateLimitDecision Allow() => new() { IsAllowed = true };

    public static BotRateLimitDecision Deny(int retryAfterSeconds) => new()
    {
        IsAllowed = false,
        RetryAfterSeconds = Math.Max(1, retryAfterSeconds)
    };
}
