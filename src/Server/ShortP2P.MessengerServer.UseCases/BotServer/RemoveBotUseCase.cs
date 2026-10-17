using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

/// <summary>Deletes the bot account and both relay queues after the bot key matches.</summary>
public sealed class RemoveBotUseCase(
    IBotAccountRepository accounts,
    IBotToClientMessageRepository toClient,
    IClientToBotMessageRepository toBot,
    IPasswordHasher passwordHasher,
    BotBlacklistGuard blacklistGuard,
    BotAccessLogService accessLog)
{
    public async Task ExecuteAsync(RemoveBotCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            await blacklistGuard
                .EnsureAllowedAsync(command.NetworkId, BotAccessOperations.Remove, cancellationToken)
                .ConfigureAwait(false);

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

            await accessLog
                .WriteAsync(
                    BotAccessOperations.Remove,
                    account.NetworkId,
                    account.BotName,
                    BotAccessLogService.OutcomeSucceeded,
                    "Bot removed.",
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (UseCaseException ex)
        {
            if (!string.Equals(ex.Code, BotAccessLogService.OutcomeForbidden, StringComparison.Ordinal))
            {
                await accessLog
                    .WriteFromExceptionAsync(
                        BotAccessOperations.Remove,
                        command.NetworkId,
                        botName: null,
                        ex,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            throw;
        }
    }
}
