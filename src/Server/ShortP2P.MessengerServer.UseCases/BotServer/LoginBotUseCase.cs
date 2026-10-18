using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

/// <summary>Issues a client-style token after the bot key, name and certificate fingerprint match.</summary>
public sealed class LoginBotUseCase(
    IBotAccountRepository accounts,
    IPasswordHasher passwordHasher,
    IAuthTokenService tokenService,
    IServerCertificateReader serverCertificate,
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
            var fingerprint = CertificateFingerprintRules.Normalize(command.FingerprintSha256);

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

            if (!CertificateFingerprintRules.AreEqual(account.CertificateFingerprintSha256, fingerprint))
                throw UseCaseException.Unauthorized("Invalid bot credentials.");

            var token = tokenService.IssueToken(account.NetworkId, BotDeviceId);
            var serverCert = await serverCertificate.GetAsync(cancellationToken).ConfigureAwait(false);

            await accessLog
                .WriteAsync(
                    BotAccessOperations.Login,
                    account.NetworkId,
                    account.BotName,
                    BotAccessLogService.OutcomeSucceeded,
                    "Bot authorized.",
                    cancellationToken)
                .ConfigureAwait(false);

            return new LoginBotResult(token.Token, token.ExpiresAtUtc, serverCert.FingerprintSha256);
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
