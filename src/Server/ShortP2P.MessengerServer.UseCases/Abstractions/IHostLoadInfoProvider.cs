namespace ShortP2P.MessengerServer.UseCases.Abstractions;

public interface IHostLoadInfoProvider
{
    Task<HostLoadInfo?> TryGetAsync(CancellationToken cancellationToken = default);
}
