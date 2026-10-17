using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>
/// In-memory bot blacklist. Not subject to message-cache TTL or memory limits.
/// When <see cref="MessengerCacheOptions.CacheEnabled"/> is true, reads use this store only.
/// </summary>
public interface IBotBlacklistCache
{
    Task AddAsync(BotBlacklistEntry entry, CancellationToken cancellationToken = default);

    Task RemoveAsync(string networkId, CancellationToken cancellationToken = default);

    Task<bool> ContainsAsync(string networkId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BotBlacklistEntry>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Replace the whole set (startup warmup from durable storage).</summary>
    Task ReplaceAllAsync(IReadOnlyList<BotBlacklistEntry> entries, CancellationToken cancellationToken = default);
}
