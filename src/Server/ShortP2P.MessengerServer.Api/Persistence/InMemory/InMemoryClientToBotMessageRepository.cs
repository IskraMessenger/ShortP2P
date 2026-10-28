using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Api.Persistence.InMemory;

public sealed class InMemoryClientToBotMessageRepository(InMemoryMessengerStore store) : IClientToBotMessageRepository
{
    public Task AddAsync(ClientToBotMessage message, CancellationToken cancellationToken = default)
    {
        store.ClientToBotMessages.TryAdd(message.MessageId, message);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.ClientToBotMessages.ContainsKey(messageId));

    public Task<ClientToBotMessage?> FindAsync(string messageId, CancellationToken cancellationToken = default)
    {
        store.ClientToBotMessages.TryGetValue(messageId, out var message);
        return Task.FromResult(message);
    }

    public Task<IReadOnlyList<ClientToBotMessage>> TakeForBotAsync(
        string botNetworkId,
        CancellationToken cancellationToken = default) =>
        TakeForBotAsync(botNetworkId, (_, _) => Task.FromResult(false), cancellationToken);

    public async Task<IReadOnlyList<ClientToBotMessage>> TakeForBotAsync(
        string botNetworkId,
        Func<string, CancellationToken, Task<bool>> keep,
        CancellationToken cancellationToken = default)
    {
        List<ClientToBotMessage> pending;
        lock (store.ClientToBotGate)
        {
            pending = store.ClientToBotMessages.Values
                .Where(m => string.Equals(m.BotNetworkId, botNetworkId, StringComparison.Ordinal))
                .OrderBy(m => m.CreatedUtc)
                .ToList();
        }

        var result = new List<ClientToBotMessage>(pending.Count);
        foreach (var message in pending)
        {
            var leave = await keep(message.MessageId, cancellationToken).ConfigureAwait(false);
            if (!leave)
            {
                lock (store.ClientToBotGate)
                    store.ClientToBotMessages.TryRemove(message.MessageId, out _);
            }

            result.Add(message);
        }

        return result;
    }

    public Task RemoveAsync(string messageId, CancellationToken cancellationToken = default)
    {
        lock (store.ClientToBotGate)
            store.ClientToBotMessages.TryRemove(messageId, out _);
        return Task.CompletedTask;
    }

    public Task RemoveForBotAsync(string botNetworkId, CancellationToken cancellationToken = default)
    {
        lock (store.ClientToBotGate)
        {
            foreach (var message in store.ClientToBotMessages.Values
                         .Where(m => string.Equals(m.BotNetworkId, botNetworkId, StringComparison.Ordinal))
                         .ToArray())
            {
                store.ClientToBotMessages.TryRemove(message.MessageId, out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        lock (store.ClientToBotGate)
        {
            foreach (var message in store.ClientToBotMessages.Values.Where(m => m.CreatedUtc < cutoffUtc).ToArray())
                store.ClientToBotMessages.TryRemove(message.MessageId, out _);
        }

        return Task.CompletedTask;
    }
}
