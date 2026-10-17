using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Persistence.Psql.Entities;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Persistence.Psql.Repositories;

public sealed class PostgresBotAccessLogRepository(MessengerDbContext db) : IBotAccessLogRepository
{
    public async Task AppendAsync(BotAccessLogEntry entry, CancellationToken cancellationToken = default)
    {
        var row = BotAccessLogRecord.FromDomain(entry);
        row.Id = 0;
        db.BotAccessLogs.Add(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
