using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotTrust;

namespace ShortP2P.MessengerServer.UseCases.BotCatalog;

/// <summary>Lists registered bots for an authorized client (display fields only).</summary>
public sealed class ListBotsUseCase(IBotAccountRepository accounts, BotTrustEngine trust)
{
    public async Task<IReadOnlyList<BotCatalogDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var all = await accounts.ListAllAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<BotCatalogDto>();
        foreach (var account in all
                     .Where(account => !string.IsNullOrWhiteSpace(account.NetworkId))
                     .OrderBy(DisplayName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(account => account.NetworkId, StringComparer.Ordinal))
        {
            // Soft-exclude low-trust bots from the catalog (banned band < 0.5).
            if (await trust.IsBannedAsync(account.NetworkId, cancellationToken).ConfigureAwait(false))
                continue;

            var dto = new BotCatalogDto
            {
                NetworkId = account.NetworkId.Trim(),
                BotName = account.BotName.Trim(),
                BotReadableName = account.BotReadableName.Trim(),
                BotDescription = account.BotDescription.Trim(),
                MessagePublicKey = account.MessagePublicKey.Trim()
            };
            if (dto.MessagePublicKey.Length == 0)
                continue;

            result.Add(dto);
        }

        return result;
    }

    private static string DisplayName(BotAccount account)
    {
        if (!string.IsNullOrWhiteSpace(account.BotReadableName))
            return account.BotReadableName.Trim();
        return account.BotName.Trim();
    }
}
