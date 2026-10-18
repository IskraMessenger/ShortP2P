using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotBlacklist;

/// <summary>
/// Routes blacklist reads/writes by <see cref="MessengerCacheOptions"/>.
/// Reads hit in-memory only when cache is enabled; otherwise durable storage only.
/// Writes update the cache immediately when enabled, and durable storage when repository is enabled.
/// </summary>
public sealed class BotBlacklistService(
    IBotBlacklistRepository repository,
    IBotBlacklistCache cache,
    MessengerCacheOptions options,
    IClock clock)
{
    public async Task<bool> ContainsAsync(string networkId, CancellationToken cancellationToken = default)
    {
        StorageAccess.EnsureAnyStoreEnabled(options);
        var id = RequireNetworkId(networkId);

        if (options.CacheEnabled)
            return await cache.ContainsAsync(id, cancellationToken).ConfigureAwait(false);

        return await repository.ContainsAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> ListNetworkIdsAsync(CancellationToken cancellationToken = default)
    {
        StorageAccess.EnsureAnyStoreEnabled(options);

        IReadOnlyList<BotBlacklistEntry> entries;
        if (options.CacheEnabled)
            entries = await cache.ListAllAsync(cancellationToken).ConfigureAwait(false);
        else
            entries = await repository.ListAllAsync(cancellationToken).ConfigureAwait(false);

        return entries
            .Select(e => e.NetworkId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task AddAsync(string networkId, CancellationToken cancellationToken = default)
    {
        StorageAccess.EnsureAnyStoreEnabled(options);
        var id = RequireNetworkId(networkId);
        var entry = new BotBlacklistEntry
        {
            NetworkId = id,
            AddedAtUtc = clock.UtcNow
        };

        if (options.CacheEnabled)
            await cache.AddAsync(entry, cancellationToken).ConfigureAwait(false);

        if (options.RepositoryEnabled)
            await repository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string networkId, CancellationToken cancellationToken = default)
    {
        StorageAccess.EnsureAnyStoreEnabled(options);
        var id = RequireNetworkId(networkId);

        if (options.CacheEnabled)
            await cache.RemoveAsync(id, cancellationToken).ConfigureAwait(false);

        if (options.RepositoryEnabled)
            await repository.RemoveAsync(id, cancellationToken).ConfigureAwait(false);
    }

    private static string RequireNetworkId(string? networkId) => networkId?.Trim() ?? "";
}
