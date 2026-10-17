using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.Hosting;

public sealed class BotToClientWaitService : PartyInboxWaitService, IBotToClientWaitService
{
    public Task WaitAsync(string clientNetworkId, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        WaitCoreAsync(clientNetworkId, timeout, cancellationToken);

    public void Notify(string clientNetworkId) => NotifyCore(clientNetworkId);
}
