namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>Bot network id and the command list last reported to this messenger server.</summary>
public sealed class BotCommandsDto
{
    public required string BotNetworkId { get; init; }

    public required IReadOnlyList<BotCommandDto> Commands { get; init; }
}
