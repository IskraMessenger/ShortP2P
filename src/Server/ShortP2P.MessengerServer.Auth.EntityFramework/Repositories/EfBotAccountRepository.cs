using Microsoft.EntityFrameworkCore;
using ShortP2P.MessengerServer.Auth.EntityFramework.Entities;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Auth.EntityFramework.Repositories;

public sealed class EfBotAccountRepository(AuthDbContext db) : IBotAccountRepository
{
    public async Task<BotAccount?> FindByNetworkIdAsync(
        string networkId,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Bots.AsNoTracking()
            .FirstOrDefaultAsync(x => x.NetworkId == networkId, cancellationToken)
            .ConfigureAwait(false);
        return row?.ToDomain();
    }

    public async Task<BotAccount?> FindByBotNameAsync(string botName, CancellationToken cancellationToken = default)
    {
        var row = await db.Bots.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BotName == botName, cancellationToken)
            .ConfigureAwait(false);
        return row?.ToDomain();
    }

    public async Task AddAsync(BotAccount account, CancellationToken cancellationToken = default)
    {
        db.Bots.Add(BotAccountEntity.FromDomain(account));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string networkId, CancellationToken cancellationToken = default)
    {
        await db.Bots
            .Where(x => x.NetworkId == networkId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
