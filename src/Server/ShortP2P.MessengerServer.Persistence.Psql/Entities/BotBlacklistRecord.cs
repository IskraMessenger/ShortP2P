using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Persistence.Psql.Entities;

public sealed class BotBlacklistRecord
{
    public string NetworkId { get; set; } = "";
    public DateTime AddedAtUtc { get; set; }

    public BotBlacklistEntry ToDomain() => new()
    {
        NetworkId = NetworkId,
        AddedAtUtc = AddedAtUtc
    };

    public static BotBlacklistRecord FromDomain(BotBlacklistEntry entry) => new()
    {
        NetworkId = entry.NetworkId,
        AddedAtUtc = entry.AddedAtUtc
    };
}
