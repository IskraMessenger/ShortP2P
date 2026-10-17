namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>Client confirms it received a bot → client message. The server then drops its copy.</summary>
public sealed class BotMessageReceiptRequest
{
    public required string MessageId { get; init; }

    /// <summary>Receipt timestamp UTC.</summary>
    public required DateTime ReceivedAtUtc { get; init; }
}
