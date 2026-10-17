using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.Hosting;

public sealed class ClientToBotWaitService : PartyInboxWaitService, IClientToBotWaitService
{
    public Task WaitAsync(string botNetworkId, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        WaitCoreAsync(botNetworkId, timeout, cancellationToken);

    public void Notify(string botNetworkId) => NotifyCore(botNetworkId);
}
