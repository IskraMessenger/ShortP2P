using System.Collections.Concurrent;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Infrastructure.Caching;

/// <summary>Process-local bot blacklist; independent of message-cache TTL and memory caps.</summary>
public sealed class InMemoryBotBlacklistCache : IBotBlacklistCache
{
    private readonly ConcurrentDictionary<string, BotBlacklistEntry> _entries =
        new(StringComparer.Ordinal);

    public Task AddAsync(BotBlacklistEntry entry, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _entries[entry.NetworkId] = entry;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string networkId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _entries.TryRemove(networkId, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ContainsAsync(string networkId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_entries.ContainsKey(networkId));
    }

    public Task<IReadOnlyList<BotBlacklistEntry>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<BotBlacklistEntry> list = _entries.Values.ToArray();
        return Task.FromResult(list);
    }

    public Task ReplaceAllAsync(IReadOnlyList<BotBlacklistEntry> entries, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _entries.Clear();
        foreach (var entry in entries)
            _entries[entry.NetworkId] = entry;
        return Task.CompletedTask;
    }
}
