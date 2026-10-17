namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>Wakes client long-polls waiting for bot → client messages. Keyed by client network id.</summary>
public interface IBotToClientWaitService
{
    Task WaitAsync(string clientNetworkId, TimeSpan timeout, CancellationToken cancellationToken = default);

    void Notify(string clientNetworkId);
}
