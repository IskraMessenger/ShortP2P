using ShortP2P.Client.Data;

namespace ShortP2P.Client.Services.MessengerServers;

public sealed class SqliteBotVersionRepository(AppDatabase appDatabase) : IBotVersionRepository
{
    private readonly AppDatabase _db = appDatabase ?? throw new ArgumentNullException(nameof(appDatabase));

    public async Task UpsertAsync(BotVersionEntity entity, CancellationToken cancellationToken = default)
    {
        Require.NotNull(entity);
        var botNetworkId = entity.BotNetworkId.Trim();
        if (entity.ServerId <= 0)
            throw new ArgumentException("ServerId is required.", nameof(entity));
        if (botNetworkId.Length == 0)
            throw new ArgumentException("BotNetworkId is required.", nameof(entity));

        await _db.WriteAsync(async conn =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rows = await conn.Table<BotVersionEntity>()
                .Where(x => x.ServerId == entity.ServerId)
                .ToListAsync()
                .ConfigureAwait(false);
            var existing = rows.FirstOrDefault(x =>
                string.Equals(x.BotNetworkId, botNetworkId, StringComparison.Ordinal));

            if (existing == null)
            {
                entity.BotNetworkId = botNetworkId;
                entity.Endpoint = entity.Endpoint.Trim();
                entity.FingerprintSha256 = entity.FingerprintSha256.Trim();
                await conn.InsertAsync(entity).ConfigureAwait(false);
                return;
            }

            existing.Version = entity.Version;
            existing.Endpoint = entity.Endpoint.Trim();
            existing.FingerprintSha256 = entity.FingerprintSha256.Trim();
            existing.UpdatedUtcTicks = entity.UpdatedUtcTicks;
            await conn.UpdateAsync(existing).ConfigureAwait(false);
            entity.Id = existing.Id;
        }).ConfigureAwait(false);
    }

    public async Task<BotVersionEntity?> FindAsync(
        int serverId,
        string botNetworkId,
        CancellationToken cancellationToken = default)
    {
        var id = botNetworkId.Trim();
        return await _db.ReadAsync(async conn =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rows = await conn.Table<BotVersionEntity>()
                .Where(x => x.ServerId == serverId)
                .ToListAsync()
                .ConfigureAwait(false);
            return rows.FirstOrDefault(x =>
                string.Equals(x.BotNetworkId, id, StringComparison.Ordinal));
        }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BotVersionEntity>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ReadAsync(async conn =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rows = await conn.Table<BotVersionEntity>().ToListAsync().ConfigureAwait(false);
            return (IReadOnlyList<BotVersionEntity>)rows;
        }).ConfigureAwait(false);
    }
}
