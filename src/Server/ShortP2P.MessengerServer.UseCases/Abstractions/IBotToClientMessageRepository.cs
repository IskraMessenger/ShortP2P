using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>Queue of encrypted bot → client messages kept until the client receipt.</summary>
public interface IBotToClientMessageRepository
{
    Task AddAsync(BotToClientMessage message, CancellationToken cancellationToken = default);

    Task<BotToClientMessage?> FindByIdAsync(string messageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BotToClientMessage>> ListForClientAsync(
        string clientNetworkId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string messageId, CancellationToken cancellationToken = default);

    Task RemoveForBotAsync(string botNetworkId, CancellationToken cancellationToken = default);

    Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
