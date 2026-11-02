namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>Client complaint about a registered bot on this messenger server.</summary>
public sealed class ClaimBotRequest
{
    public required string BotNetworkId { get; init; }
}
