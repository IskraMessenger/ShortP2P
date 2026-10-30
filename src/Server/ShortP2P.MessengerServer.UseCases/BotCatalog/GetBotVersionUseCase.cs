using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotCatalog;

/// <summary>Returns the version a bot last reported to this server.</summary>
public sealed class GetBotVersionUseCase(IBotAccountRepository accounts)
{
    public async Task<BotVersionDto> ExecuteAsync(
        GetBotVersionQuery query,
        CancellationToken cancellationToken = default)
    {
        var networkId = query.BotNetworkId.Trim();
        var account = await accounts.FindByNetworkIdAsync(networkId, cancellationToken).ConfigureAwait(false);
        if (account is null)
            throw UseCaseException.NotFound("Bot was not found.");

        return new BotVersionDto
        {
            BotNetworkId = account.NetworkId.Trim(),
            Version = account.Version
        };
    }
}
