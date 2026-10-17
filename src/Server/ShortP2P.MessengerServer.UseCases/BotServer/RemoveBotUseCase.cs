using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

/// <summary>Deletes the bot account and both relay queues after the bot key matches.</summary>
public sealed class RemoveBotUseCase(
    IBotAccountRepository accounts,
    IBotToClientMessageRepository toClient,
    IClientToBotMessageRepository toBot,
    IPasswordHasher passwordHasher)
{
    public async Task ExecuteAsync(RemoveBotCommand command, CancellationToken cancellationToken = default)
    {
        var account = await BotAuthentication.AuthenticateAsync(
            accounts,
            passwordHasher,
            command.NetworkId,
            command.BotKey,
            botName: null,
            requireBotName: false,
            cancellationToken).ConfigureAwait(false);

        await accounts.RemoveAsync(account.NetworkId, cancellationToken).ConfigureAwait(false);
        await toClient.RemoveForBotAsync(account.NetworkId, cancellationToken).ConfigureAwait(false);
        await toBot.RemoveForBotAsync(account.NetworkId, cancellationToken).ConfigureAwait(false);
    }
}
