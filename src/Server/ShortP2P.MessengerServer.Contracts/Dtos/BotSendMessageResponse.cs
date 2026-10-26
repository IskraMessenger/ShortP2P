namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>Acknowledgement for <see cref="BotSendMessageRequest"/>.</summary>
public sealed class BotSendMessageResponse
{
    /// <summary>Echo of the request message id.</summary>
    public required string MessageId { get; init; }

    /// <summary>Outcome of the send.</summary>
    public required ErrorCode ErrorCode { get; init; }

    /// <summary>Human-readable error detail; empty on success.</summary>
    public required string ErrorMessage { get; init; }
}
