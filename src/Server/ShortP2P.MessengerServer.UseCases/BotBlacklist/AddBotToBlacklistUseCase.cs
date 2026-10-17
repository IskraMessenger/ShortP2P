namespace ShortP2P.MessengerServer.UseCases.BotBlacklist;

public sealed class AddBotToBlacklistUseCase(BotBlacklistService blacklist)
{
    public Task ExecuteAsync(AddBotToBlacklistCommand command, CancellationToken cancellationToken = default) =>
        blacklist.AddAsync(command.NetworkId, cancellationToken);
}
