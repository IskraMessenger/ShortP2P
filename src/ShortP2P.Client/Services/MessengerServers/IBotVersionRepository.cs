using ShortP2P.Client.Data;

namespace ShortP2P.Client.Services.MessengerServers;

public interface IBotVersionRepository
{
    /// <summary>
    /// Inserts or replaces the version for <see cref="BotVersionEntity.ServerId"/> and
    /// <see cref="BotVersionEntity.BotNetworkId"/>.
    /// </summary>
    Task UpsertAsync(BotVersionEntity entity, CancellationToken cancellationToken = default);

    Task<BotVersionEntity?> FindAsync(
        int serverId,
        string botNetworkId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BotVersionEntity>> ListAllAsync(CancellationToken cancellationToken = default);
}
