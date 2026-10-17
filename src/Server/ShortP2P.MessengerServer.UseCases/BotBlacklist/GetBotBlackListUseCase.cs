namespace ShortP2P.MessengerServer.UseCases.BotBlacklist;

/// <summary>Returns blacklisted bot network ids for authorized clients.</summary>
public sealed class GetBotBlackListUseCase(BotBlacklistService blacklist)
{
    public Task<IReadOnlyList<string>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        blacklist.ListNetworkIdsAsync(cancellationToken);
}
