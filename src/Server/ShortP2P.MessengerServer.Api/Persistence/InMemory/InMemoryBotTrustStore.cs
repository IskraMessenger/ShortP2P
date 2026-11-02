using System.Collections.Concurrent;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Api.Persistence.InMemory;

public sealed class InMemoryBotTrustStore : IBotTrustStore
{
    private readonly ConcurrentDictionary<string, BotTrustState> _items = new(StringComparer.Ordinal);

    public Task<BotTrustState?> GetAsync(string botNetworkId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _items.TryGetValue(botNetworkId.Trim(), out var state) ? state.Clone() : null);
    }

    public Task UpsertAsync(BotTrustState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        cancellationToken.ThrowIfCancellationRequested();
        _items[state.BotNetworkId.Trim()] = state.Clone();
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string botNetworkId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items.TryRemove(botNetworkId.Trim(), out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BotTrustState>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<BotTrustState> list = _items.Values
            .Select(s => s.Clone())
            .OrderBy(s => s.BotNetworkId, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(list);
    }
}
