namespace ShortP2P.MessengerServer.UseCases.BotBlacklist;

public sealed class RemoveBotFromBlacklistUseCase(BotBlacklistService blacklist)
{
    public Task ExecuteAsync(RemoveBotFromBlacklistCommand command, CancellationToken cancellationToken = default) =>
        blacklist.RemoveAsync(command.NetworkId, cancellationToken);
}
