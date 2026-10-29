namespace ShortP2P.MessengerServer.UseCases;

/// <summary>Application-level failure with a stable error code.</summary>
public sealed class UseCaseException : Exception
{
    public UseCaseException(string code, string message, int? retryAfterSeconds = null)
        : base(message)
    {
        Code = code;
        RetryAfterSeconds = retryAfterSeconds;
    }

    public string Code { get; }

    /// <summary>Optional Retry-After hint (seconds), used for TooManyRequests.</summary>
    public int? RetryAfterSeconds { get; }

    public static UseCaseException Validation(string message) => new("Validation", message);

    public static UseCaseException Conflict(string message) => new("Conflict", message);

    public static UseCaseException PeerOffline(string message) => new("PeerOffline", message);

    public static UseCaseException PayloadTooLarge(string message) => new("PayloadTooLarge", message);

    public static UseCaseException NotFound(string message) => new("NotFound", message);

    public static UseCaseException Unauthorized(string message) => new("Unauthorized", message);

    public static UseCaseException Forbidden(string message) => new("Forbidden", message);

    public static UseCaseException Unavailable(string message) => new("Unavailable", message);

    public static UseCaseException InsufficientStorage(string message) => new("InsufficientStorage", message);

    public static UseCaseException TooManyRequests(string message, int? retryAfterSeconds = null) =>
        new("TooManyRequests", message, retryAfterSeconds);
}
