using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>Queue of encrypted client → bot messages. A take removes them (delivered to the bot).</summary>
public interface IClientToBotMessageRepository
{
    Task AddAsync(ClientToBotMessage message, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default);

    Task<ClientToBotMessage?> FindAsync(string messageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientToBotMessage>> TakeForBotAsync(
        string botNetworkId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every pending message for the bot. A message stays in the queue when <paramref name="keep"/> is true.
    /// </summary>
    Task<IReadOnlyList<ClientToBotMessage>> TakeForBotAsync(
        string botNetworkId,
        Func<string, CancellationToken, Task<bool>> keep,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string messageId, CancellationToken cancellationToken = default);

    Task RemoveForBotAsync(string botNetworkId, CancellationToken cancellationToken = default);

    Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
