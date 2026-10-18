using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

/// <summary>Creates a bot account and returns the bot key once. Only the key hash is stored.</summary>
public sealed class RegisterBotUseCase(
    IBotAccountRepository accounts,
    IBotKeyGenerator keyGenerator,
    IPasswordHasher passwordHasher,
    IClock clock,
    IServerCertificateReader serverCertificate,
    BotBlacklistGuard blacklistGuard,
    BotAccessLogService accessLog)
{
    public async Task<RegisterBotResult> ExecuteAsync(
        RegisterBotCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var networkId = InputGuard.NetworkId(command.NetworkId);
            var botName = InputGuard.Cleartext(command.BotName, BotLimits.MaxBotNameLength, "botName");
            var botReadableName = InputGuard.Cleartext(
                command.BotReadableName, BotLimits.MaxBotReadableNameLength, "botReadableName");
            var botDescription = InputGuard.Cleartext(
                command.BotDescription, BotLimits.MaxBotDescriptionLength, "botDescription");
            var fingerprint = InputGuard.CertificateFingerprintSha256(command.FingerprintSha256);

            await blacklistGuard
                .EnsureAllowedAsync(networkId, BotAccessOperations.Register, cancellationToken)
                .ConfigureAwait(false);

            if (await accounts.FindByNetworkIdAsync(networkId, cancellationToken).ConfigureAwait(false) is not null)
                throw UseCaseException.Conflict("NetworkId is already registered.");

            if (await accounts.FindByBotNameAsync(botName, cancellationToken).ConfigureAwait(false) is not null)
                throw UseCaseException.Conflict("BotName is already registered.");

            var botKey = keyGenerator.Generate();
            var hashed = passwordHasher.Hash(botKey);

            try
            {
                await accounts.AddAsync(
                    new BotAccount
                    {
                        NetworkId = networkId,
                        BotName = botName,
                        BotReadableName = botReadableName,
                        BotDescription = botDescription,
                        KeySalt = hashed.Salt,
                        KeyHash = hashed.Hash,
                        CertificateFingerprintSha256 = fingerprint,
                        CreatedAtUtc = clock.UtcNow
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                throw UseCaseException.Conflict("NetworkId or botName is already registered.");
            }

            var serverCert = await serverCertificate.GetAsync(cancellationToken).ConfigureAwait(false);

            await accessLog
                .WriteAsync(
                    BotAccessOperations.Register,
                    networkId,
                    botName,
                    BotAccessLogService.OutcomeSucceeded,
                    "Bot registered.",
                    cancellationToken)
                .ConfigureAwait(false);

            return new RegisterBotResult(
                networkId,
                botName,
                botReadableName,
                botDescription,
                botKey,
                serverCert.FingerprintSha256);
        }
        catch (UseCaseException ex)
        {
            if (!string.Equals(ex.Code, BotAccessLogService.OutcomeForbidden, StringComparison.Ordinal))
            {
                await accessLog
                    .WriteFromExceptionAsync(
                        BotAccessOperations.Register,
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
