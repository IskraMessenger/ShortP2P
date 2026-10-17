namespace ShortP2P.MessengerServer.Domain;

/// <summary>Registered bot on this messenger server. The bot key itself is never stored, only its hash.</summary>
public sealed class BotAccount
{
    public required string NetworkId { get; init; }

    public required string BotName { get; init; }

    public required string BotReadableName { get; init; }

    public required string BotDescription { get; init; }

    public required string KeySalt { get; init; }

    public required string KeyHash { get; init; }

    /// <summary>Creation time UTC.</summary>
    public required DateTime CreatedAtUtc { get; init; }
}
