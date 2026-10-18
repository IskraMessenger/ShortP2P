using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

/// <summary>
/// Shared bot-key check. Failures share one message so callers cannot tell which part missed.
/// Input shape is validated at the API via FluentValidation.
/// </summary>
internal static class BotAuthentication
{
    public static async Task<BotAccount> AuthenticateAsync(
        IBotAccountRepository accounts,
        IPasswordHasher passwordHasher,
        string? networkId,
        string? botKey,
        string? botName,
        bool requireBotName,
        CancellationToken cancellationToken)
    {
        var id = networkId?.Trim() ?? "";
        var key = botKey?.Trim() ?? "";
        var name = botName?.Trim() ?? "";

        var account = await accounts
            .FindByNetworkIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (account is null)
            throw UseCaseException.Unauthorized("Invalid bot credentials.");

        var keyMatches = passwordHasher.Verify(key, account.KeySalt, account.KeyHash);
        var nameMatches = !requireBotName
            || string.Equals(account.BotName, name, StringComparison.Ordinal);
        if (!keyMatches || !nameMatches)
            throw UseCaseException.Unauthorized("Invalid bot credentials.");

        return account;
    }
}
