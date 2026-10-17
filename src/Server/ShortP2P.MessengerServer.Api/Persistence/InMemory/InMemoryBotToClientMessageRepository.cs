using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Api.Persistence.InMemory;

public sealed class InMemoryBotToClientMessageRepository(InMemoryMessengerStore store) : IBotToClientMessageRepository
{
    public Task AddAsync(BotToClientMessage message, CancellationToken cancellationToken = default)
    {
        store.BotToClientMessages[message.MessageId] = message;
        return Task.CompletedTask;
    }

    public Task<BotToClientMessage?> FindByIdAsync(string messageId, CancellationToken cancellationToken = default)
    {
        store.BotToClientMessages.TryGetValue(messageId, out var message);
        return Task.FromResult(message);
    }

    public Task<IReadOnlyList<BotToClientMessage>> ListForClientAsync(
        string clientNetworkId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BotToClientMessage> list = store.BotToClientMessages.Values
            .Where(m => string.Equals(m.ClientNetworkId, clientNetworkId, StringComparison.Ordinal))
            .OrderBy(m => m.CreatedUtc)
            .ToArray();
        return Task.FromResult(list);
    }

    public Task RemoveAsync(string messageId, CancellationToken cancellationToken = default)
    {
        store.BotToClientMessages.TryRemove(messageId, out _);
        return Task.CompletedTask;
    }

    public Task RemoveForBotAsync(string botNetworkId, CancellationToken cancellationToken = default)
    {
        foreach (var message in store.BotToClientMessages.Values
                     .Where(m => string.Equals(m.BotNetworkId, botNetworkId, StringComparison.Ordinal))
                     .ToArray())
        {
            store.BotToClientMessages.TryRemove(message.MessageId, out _);
        }

        return Task.CompletedTask;
    }

    public Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        foreach (var message in store.BotToClientMessages.Values.Where(m => m.CreatedUtc < cutoffUtc).ToArray())
            store.BotToClientMessages.TryRemove(message.MessageId, out _);

        return Task.CompletedTask;
    }
}
