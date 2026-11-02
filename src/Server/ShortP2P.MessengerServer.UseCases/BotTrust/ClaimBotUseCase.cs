using Microsoft.Extensions.Logging;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotTrust;

/// <summary>Records a client claim against a bot. Rating is adjusted later by the antifraud hosted service.</summary>
public sealed class ClaimBotUseCase(
    BotTrustEngine trust,
    IBotAccountRepository bots,
    ILogger<ClaimBotUseCase> logger)
{
    public async Task ExecuteAsync(ClaimBotCommand command, CancellationToken cancellationToken = default)
    {
        var caller = command.CallerNetworkId.Trim();
        var botNetworkId = command.BotNetworkId.Trim();

        if (await bots.FindByNetworkIdAsync(botNetworkId, cancellationToken).ConfigureAwait(false) is null)
            throw UseCaseException.NotFound("Bot is not registered.");

        try
        {
            await trust.EnsurePublishedAsync(botNetworkId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await trust.AddClaimAsync(botNetworkId, caller, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Bot trust claim ingested bot={BotNetworkId} complainant={Complainant}",
                botNetworkId,
                caller);
        }
        catch (InvalidOperationException ex)
        {
            throw UseCaseException.Validation(ex.Message);
        }
    }
}
