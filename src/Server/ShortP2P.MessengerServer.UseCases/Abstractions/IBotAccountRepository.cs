using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.Abstractions;

public interface IBotAccountRepository
{
    Task<BotAccount?> FindByNetworkIdAsync(string networkId, CancellationToken cancellationToken = default);

    Task<BotAccount?> FindByBotNameAsync(string botName, CancellationToken cancellationToken = default);

    /// <summary>Inserts the account. Throws <see cref="InvalidOperationException"/> when network id or bot name already exists.</summary>
    Task AddAsync(BotAccount account, CancellationToken cancellationToken = default);

    Task RemoveAsync(string networkId, CancellationToken cancellationToken = default);
}
