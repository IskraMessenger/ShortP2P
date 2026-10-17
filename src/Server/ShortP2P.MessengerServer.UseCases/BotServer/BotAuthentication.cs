using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

/// <summary>
/// Shared bot-key check. Failures share one message so callers cannot tell which part missed.
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
        if (string.IsNullOrWhiteSpace(networkId)
            || string.IsNullOrWhiteSpace(botKey)
            || (requireBotName && string.IsNullOrWhiteSpace(botName)))
        {
            throw UseCaseException.Validation(
                requireBotName
                    ? "NetworkId, botName and botKey are required."
                    : "NetworkId and botKey are required.");
        }

        var key = botKey.Trim();
        if (key.Length != BotLimits.BotKeyLength)
            throw UseCaseException.Validation($"botKey must be {BotLimits.BotKeyLength} characters.");

        var account = await accounts
            .FindByNetworkIdAsync(networkId.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (account is null)
            throw UseCaseException.Unauthorized("Invalid bot credentials.");

        var keyMatches = passwordHasher.Verify(key, account.KeySalt, account.KeyHash);
        var nameMatches = !requireBotName
            || string.Equals(account.BotName, botName!.Trim(), StringComparison.Ordinal);
        if (!keyMatches || !nameMatches)
            throw UseCaseException.Unauthorized("Invalid bot credentials.");

        return account;
    }
}
