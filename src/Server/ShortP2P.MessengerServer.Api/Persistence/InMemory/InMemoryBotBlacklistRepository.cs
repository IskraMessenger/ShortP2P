using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Api.Persistence.InMemory;

public sealed class InMemoryBotBlacklistRepository(InMemoryMessengerStore store) : IBotBlacklistRepository
{
    public Task AddAsync(BotBlacklistEntry entry, CancellationToken cancellationToken = default)
    {
        store.BotBlacklist[entry.NetworkId] = entry;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string networkId, CancellationToken cancellationToken = default)
    {
        store.BotBlacklist.TryRemove(networkId, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ContainsAsync(string networkId, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.BotBlacklist.ContainsKey(networkId));

    public Task<IReadOnlyList<BotBlacklistEntry>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BotBlacklistEntry> list = store.BotBlacklist.Values.ToArray();
        return Task.FromResult(list);
    }
}
