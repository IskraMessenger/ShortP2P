using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>Queue of encrypted client → bot messages. A take removes them (delivered to the bot).</summary>
public interface IClientToBotMessageRepository
{
    Task AddAsync(ClientToBotMessage message, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientToBotMessage>> TakeForBotAsync(
        string botNetworkId,
        CancellationToken cancellationToken = default);

    Task RemoveForBotAsync(string botNetworkId, CancellationToken cancellationToken = default);

    Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
