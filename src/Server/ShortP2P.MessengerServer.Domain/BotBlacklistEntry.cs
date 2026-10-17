namespace ShortP2P.MessengerServer.Domain;

/// <summary>Bot network id blocked on this messenger server.</summary>
public sealed class BotBlacklistEntry
{
    public required string NetworkId { get; init; }

    /// <summary>When the bot was added to the blacklist (UTC).</summary>
    public required DateTime AddedAtUtc { get; init; }
}
