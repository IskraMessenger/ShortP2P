namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>Wakes bot long-polls waiting for client → bot messages. Keyed by bot network id.</summary>
public interface IClientToBotWaitService
{
    Task WaitAsync(string botNetworkId, TimeSpan timeout, CancellationToken cancellationToken = default);

    void Notify(string botNetworkId);
}
