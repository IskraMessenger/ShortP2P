using Microsoft.EntityFrameworkCore;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Persistence.Psql.Entities;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Persistence.Psql.Repositories;

public sealed class PostgresBotBlacklistRepository(MessengerDbContext db) : IBotBlacklistRepository
{
    public async Task AddAsync(BotBlacklistEntry entry, CancellationToken cancellationToken = default)
    {
        var exists = await db.BotBlacklist
            .AnyAsync(x => x.NetworkId == entry.NetworkId, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
            return;

        db.BotBlacklist.Add(BotBlacklistRecord.FromDomain(entry));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string networkId, CancellationToken cancellationToken = default)
    {
        await db.BotBlacklist
            .Where(x => x.NetworkId == networkId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> ContainsAsync(string networkId, CancellationToken cancellationToken = default) =>
        db.BotBlacklist.AsNoTracking().AnyAsync(x => x.NetworkId == networkId, cancellationToken);

    public async Task<IReadOnlyList<BotBlacklistEntry>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.BotBlacklist.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(x => x.ToDomain()).ToArray();
    }
}
