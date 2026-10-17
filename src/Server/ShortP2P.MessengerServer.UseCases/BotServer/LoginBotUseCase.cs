using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

/// <summary>Issues a client-style token after the bot key and bot name match the stored account.</summary>
public sealed class LoginBotUseCase(
    IBotAccountRepository accounts,
    IPasswordHasher passwordHasher,
    IAuthTokenService tokenService)
{
    /// <summary>Bots are not device-bound; tokens use an all-zero device id placeholder.</summary>
    private static readonly string BotDeviceId = new string('0', DeviceIdRules.Length);

    public async Task<LoginBotResult> ExecuteAsync(
        LoginBotCommand command,
        CancellationToken cancellationToken = default)
    {
        var account = await BotAuthentication.AuthenticateAsync(
            accounts,
            passwordHasher,
            command.NetworkId,
            command.BotKey,
            command.BotName,
            requireBotName: true,
            cancellationToken).ConfigureAwait(false);

        var token = tokenService.IssueToken(account.NetworkId, BotDeviceId);
        return new LoginBotResult(token.Token, token.ExpiresAtUtc);
    }
}
