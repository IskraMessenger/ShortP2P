using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.Abstractions;

public interface ICryptoKeysRepository
{
    Task UpsertAsync(CryptoKeys keys, CancellationToken cancellationToken = default);

    /// <summary>Public key published by <paramref name="srcNetworkId"/> for peer <paramref name="tgtNetworkId"/> (ChatRequest).</summary>
    Task<CryptoKeys?> FindAsync(
        string srcNetworkId,
        string tgtNetworkId,
        CancellationToken cancellationToken = default);
}
