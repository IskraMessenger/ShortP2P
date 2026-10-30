namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>Bot network id and the version last reported to this messenger server.</summary>
public sealed class BotVersionDto
{
    public required string BotNetworkId { get; init; }

    public required int Version { get; init; }
}
