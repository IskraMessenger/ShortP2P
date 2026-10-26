using Microsoft.EntityFrameworkCore;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Persistence.Psql.Entities;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Persistence.Psql.Repositories;

public sealed class PostgresCryptoKeysRepository(MessengerDbContext db) : ICryptoKeysRepository
{
    public async Task UpsertAsync(CryptoKeys keys, CancellationToken cancellationToken = default)
    {
        var existing = await db.CryptoKeys
            .FirstOrDefaultAsync(
                x => x.SrcNetworkId == keys.SrcNetworkId && x.TgtNetworkId == keys.TgtNetworkId,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
            db.CryptoKeys.Add(CryptoKeysRecord.FromDomain(keys));
        else
            existing.PublicKey = keys.PublicKey;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<CryptoKeys?> FindAsync(
        string srcNetworkId,
        string tgtNetworkId,
        CancellationToken cancellationToken = default)
    {
        var src = srcNetworkId.Trim();
        var tgt = tgtNetworkId.Trim();
        var row = await db.CryptoKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SrcNetworkId == src && x.TgtNetworkId == tgt, cancellationToken)
            .ConfigureAwait(false);
        return row?.ToDomain();
    }
}
