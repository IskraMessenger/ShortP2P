using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

/// <summary>Issues a client-style token after the bot key and bot name match the stored account.</summary>
public sealed class LoginBotUseCase(
    IBotAccountRepository accounts,
    IPasswordHasher passwordHasher,
    IAuthTokenService tokenService,
    BotBlacklistGuard blacklistGuard,
    BotAccessLogService accessLog)
{
    /// <summary>Bots are not device-bound; tokens use an all-zero device id placeholder.</summary>
    private static readonly string BotDeviceId = new string('0', DeviceIdRules.Length);

    public async Task<LoginBotResult> ExecuteAsync(
        LoginBotCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await blacklistGuard
                .EnsureAllowedAsync(command.NetworkId, BotAccessOperations.Login, cancellationToken)
                .ConfigureAwait(false);

            var account = await BotAuthentication.AuthenticateAsync(
                accounts,
                passwordHasher,
                command.NetworkId,
                command.BotKey,
                command.BotName,
                requireBotName: true,
                cancellationToken).ConfigureAwait(false);

            var token = tokenService.IssueToken(account.NetworkId, BotDeviceId);

            await accessLog
                .WriteAsync(
                    BotAccessOperations.Login,
                    account.NetworkId,
                    account.BotName,
                    BotAccessLogService.OutcomeSucceeded,
                    "Bot authorized.",
                    cancellationToken)
                .ConfigureAwait(false);

            return new LoginBotResult(token.Token, token.ExpiresAtUtc);
        }
        catch (UseCaseException ex)
        {
            if (!string.Equals(ex.Code, BotAccessLogService.OutcomeForbidden, StringComparison.Ordinal))
            {
                await accessLog
                    .WriteFromExceptionAsync(
                        BotAccessOperations.Login,
                        command.NetworkId,
                        command.BotName,
                        ex,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            throw;
        }
    }
}
