using Microsoft.EntityFrameworkCore;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Persistence.Psql.Entities;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Persistence.Psql.Repositories;

public sealed class PostgresBotToClientMessageRepository(MessengerDbContext db) : IBotToClientMessageRepository
{
    public async Task AddAsync(BotToClientMessage message, CancellationToken cancellationToken = default)
    {
        db.BotToClientMessages.Add(BotToClientMessageRecord.FromDomain(message));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<BotToClientMessage?> FindByIdAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        var row = await db.BotToClientMessages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.MessageId == messageId, cancellationToken)
            .ConfigureAwait(false);
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<BotToClientMessage>> ListForClientAsync(
        string clientNetworkId,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.BotToClientMessages.AsNoTracking()
            .Where(x => x.ClientNetworkId == clientNetworkId)
            .OrderBy(x => x.CreatedUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(x => x.ToDomain()).ToArray();
    }

    public async Task RemoveAsync(string messageId, CancellationToken cancellationToken = default)
    {
        await db.BotToClientMessages
            .Where(x => x.MessageId == messageId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RemoveForBotAsync(string botNetworkId, CancellationToken cancellationToken = default)
    {
        await db.BotToClientMessages
            .Where(x => x.BotNetworkId == botNetworkId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        await db.BotToClientMessages
            .Where(x => x.CreatedUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
