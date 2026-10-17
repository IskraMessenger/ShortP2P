using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Persistence.Psql.Entities;

public sealed class BotAccessLogRecord
{
    public long Id { get; set; }
    public DateTime AtUtc { get; set; }
    public string NetworkId { get; set; } = "";
    public string? BotName { get; set; }
    public string Operation { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string Detail { get; set; } = "";

    public BotAccessLogEntry ToDomain() => new()
    {
        Id = Id,
        AtUtc = AtUtc,
        NetworkId = NetworkId,
        BotName = BotName,
        Operation = Operation,
        Outcome = Outcome,
        Detail = Detail
    };

    public static BotAccessLogRecord FromDomain(BotAccessLogEntry entry) => new()
    {
        Id = entry.Id,
        AtUtc = entry.AtUtc,
        NetworkId = entry.NetworkId,
        BotName = entry.BotName,
        Operation = entry.Operation,
        Outcome = entry.Outcome,
        Detail = entry.Detail
    };
}
