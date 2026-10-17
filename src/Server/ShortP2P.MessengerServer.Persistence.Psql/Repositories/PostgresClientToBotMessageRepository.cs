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

    public async Task<IReadOnlyList<ClientToBotMessage>> TakeForBotAsync(
        string botNetworkId,
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

        db.ClientToBotMessages.RemoveRange(rows);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(x => x.ToDomain()).ToArray();
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
