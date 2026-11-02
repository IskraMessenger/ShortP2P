namespace ShortP2P.MessengerServer.UseCases.BotTrust;

public sealed class BotTrustProcessEvent
{
    public required string BotNetworkId { get; init; }

    public required BotTrustProcessEventKind Kind { get; init; }

    public required string Message { get; init; }

    public float? OldRating { get; init; }

    public float? NewRating { get; init; }
}
