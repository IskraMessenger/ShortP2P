using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotCatalog;

/// <summary>Lists registered bots for an authorized client (display fields only).</summary>
public sealed class ListBotsUseCase(IBotAccountRepository accounts)
{
    public async Task<IReadOnlyList<BotCatalogDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var all = await accounts.ListAllAsync(cancellationToken).ConfigureAwait(false);
        return all
            .Where(account => !string.IsNullOrWhiteSpace(account.NetworkId))
            .OrderBy(DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(account => account.NetworkId, StringComparer.Ordinal)
            .Select(account => new BotCatalogDto
            {
                NetworkId = account.NetworkId.Trim(),
                BotName = account.BotName.Trim(),
                BotReadableName = account.BotReadableName.Trim(),
                BotDescription = account.BotDescription.Trim()
            })
            .ToArray();
    }

    private static string DisplayName(BotAccount account)
    {
        if (!string.IsNullOrWhiteSpace(account.BotReadableName))
            return account.BotReadableName.Trim();
        return account.BotName.Trim();
    }
}
