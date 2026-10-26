using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Api.Persistence.InMemory;

public sealed class InMemoryCryptoKeysRepository(InMemoryMessengerStore store) : ICryptoKeysRepository
{
    public Task UpsertAsync(CryptoKeys keys, CancellationToken cancellationToken = default)
    {
        store.CryptoKeys[(keys.SrcNetworkId, keys.TgtNetworkId)] = keys;
        return Task.CompletedTask;
    }

    public Task<CryptoKeys?> FindAsync(
        string srcNetworkId,
        string tgtNetworkId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        store.CryptoKeys.TryGetValue((srcNetworkId.Trim(), tgtNetworkId.Trim()), out var keys);
        return Task.FromResult(keys);
    }
}
