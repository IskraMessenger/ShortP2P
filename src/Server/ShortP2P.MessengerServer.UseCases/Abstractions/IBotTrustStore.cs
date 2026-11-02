using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>Durable bot trust scores and pending claims for this messenger server.</summary>
public interface IBotTrustStore
{
    Task<BotTrustState?> GetAsync(string botNetworkId, CancellationToken cancellationToken = default);

    Task UpsertAsync(BotTrustState state, CancellationToken cancellationToken = default);

    Task RemoveAsync(string botNetworkId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BotTrustState>> ListAsync(CancellationToken cancellationToken = default);
}
