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

    public Task<IReadOnlyList<ClientToBotMessage>> TakeForBotAsync(
        string botNetworkId,
        CancellationToken cancellationToken = default)
    {
        lock (store.ClientToBotGate)
        {
            var taken = store.ClientToBotMessages.Values
                .Where(m => string.Equals(m.BotNetworkId, botNetworkId, StringComparison.Ordinal))
                .OrderBy(m => m.CreatedUtc)
                .ToArray();

            foreach (var message in taken)
                store.ClientToBotMessages.TryRemove(message.MessageId, out _);

            return Task.FromResult<IReadOnlyList<ClientToBotMessage>>(taken);
        }
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
