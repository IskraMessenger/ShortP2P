using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>Durable bot blacklist (Postgres or in-memory stand-in).</summary>
public interface IBotBlacklistRepository
{
    Task AddAsync(BotBlacklistEntry entry, CancellationToken cancellationToken = default);

    Task RemoveAsync(string networkId, CancellationToken cancellationToken = default);

    Task<bool> ContainsAsync(string networkId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BotBlacklistEntry>> ListAllAsync(CancellationToken cancellationToken = default);
}
