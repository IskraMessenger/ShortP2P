namespace ShortP2P.MessengerServer.UseCases.Abstractions;

public interface IHostHardwareInfoProvider
{
    Task<HostHardwareInfo?> TryGetAsync(CancellationToken cancellationToken = default);
}
