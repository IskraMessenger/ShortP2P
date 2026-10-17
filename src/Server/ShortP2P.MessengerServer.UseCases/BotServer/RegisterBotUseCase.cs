using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

/// <summary>Creates a bot account and returns the bot key once. Only the key hash is stored.</summary>
public sealed class RegisterBotUseCase(
    IBotAccountRepository accounts,
    IBotKeyGenerator keyGenerator,
    IPasswordHasher passwordHasher,
    IClock clock)
{
    public async Task<RegisterBotResult> ExecuteAsync(
        RegisterBotCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.NetworkId)
            || string.IsNullOrWhiteSpace(command.BotName)
            || string.IsNullOrWhiteSpace(command.BotReadableName)
            || string.IsNullOrWhiteSpace(command.BotDescription))
        {
            throw UseCaseException.Validation(
                "NetworkId, botName, botReadableName and botDescription are required.");
        }

        var networkId = command.NetworkId.Trim();
        var botName = command.BotName.Trim();
        var botReadableName = command.BotReadableName.Trim();
        var botDescription = command.BotDescription.Trim();

        if (botName.Length > BotLimits.MaxBotNameLength)
            throw UseCaseException.Validation($"botName must be at most {BotLimits.MaxBotNameLength} characters.");

        if (botReadableName.Length > BotLimits.MaxBotReadableNameLength)
        {
            throw UseCaseException.Validation(
                $"botReadableName must be at most {BotLimits.MaxBotReadableNameLength} characters.");
        }

        if (botDescription.Length > BotLimits.MaxBotDescriptionLength)
        {
            throw UseCaseException.Validation(
                $"botDescription must be at most {BotLimits.MaxBotDescriptionLength} characters.");
        }

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
                    CreatedAtUtc = clock.UtcNow
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            throw UseCaseException.Conflict("NetworkId or botName is already registered.");
        }

        return new RegisterBotResult(networkId, botName, botReadableName, botDescription, botKey);
    }
}
