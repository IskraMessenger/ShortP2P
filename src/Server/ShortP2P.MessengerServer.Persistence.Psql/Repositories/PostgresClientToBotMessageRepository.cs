using Microsoft.EntityFrameworkCore;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Persistence.Psql.Entities;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Persistence.Psql.Repositories;

public sealed class PostgresClientToBotMessageRepository(MessengerDbContext db) : IClientToBotMessageRepository
{
    public async Task AddAsync(ClientToBotMessage message, CancellationToken cancellationToken = default)
    {
        db.ClientToBotMessages.Add(ClientToBotMessageRecord.FromDomain(message));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default) =>
        db.ClientToBotMessages.AsNoTracking().AnyAsync(x => x.MessageId == messageId, cancellationToken);

    public async Task<ClientToBotMessage?> FindAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var row = await db.ClientToBotMessages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.MessageId == messageId, cancellationToken)
            .ConfigureAwait(false);
        return row?.ToDomain();
    }

    public Task<IReadOnlyList<ClientToBotMessage>> TakeForBotAsync(
        string botNetworkId,
        CancellationToken cancellationToken = default) =>
        TakeForBotAsync(botNetworkId, (_, _) => Task.FromResult(false), cancellationToken);

    public async Task<IReadOnlyList<ClientToBotMessage>> TakeForBotAsync(
        string botNetworkId,
        Func<string, CancellationToken, Task<bool>> keep,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.ClientToBotMessages
            .Where(x => x.BotNetworkId == botNetworkId)
            .OrderBy(x => x.CreatedUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return [];
        }

        foreach (var row in rows)
        {
            if (!await keep(row.MessageId, cancellationToken).ConfigureAwait(false))
                db.ClientToBotMessages.Remove(row);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(x => x.ToDomain()).ToArray();
    }

    public async Task RemoveAsync(string messageId, CancellationToken cancellationToken = default)
    {
        await db.ClientToBotMessages
            .Where(x => x.MessageId == messageId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RemoveForBotAsync(string botNetworkId, CancellationToken cancellationToken = default)
    {
        await db.ClientToBotMessages
            .Where(x => x.BotNetworkId == botNetworkId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        await db.ClientToBotMessages
            .Where(x => x.CreatedUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
