using Microsoft.Extensions.Logging;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotBlacklist;

/// <summary>
/// Writes <c>bot_access_log</c> entries. Forbidden (403) attempts also go to the ordinary application log.
/// </summary>
public sealed class BotAccessLogService(
    IBotAccessLogRepository repository,
    IClock clock,
    ILogger<BotAccessLogService> logger)
{
    public const string OutcomeSucceeded = "Succeeded";
    public const string OutcomeForbidden = "Forbidden";

    public async Task WriteAsync(
        string operation,
        string? networkId,
        string? botName,
        string outcome,
        string detail,
        CancellationToken cancellationToken = default)
    {
        var id = string.IsNullOrWhiteSpace(networkId) ? string.Empty : networkId.Trim();
        var entry = new BotAccessLogEntry
        {
            Id = 0,
            AtUtc = clock.UtcNow,
            NetworkId = id,
            BotName = string.IsNullOrWhiteSpace(botName) ? null : botName.Trim(),
            Operation = operation,
            Outcome = outcome,
            Detail = detail
        };

        if (string.Equals(outcome, OutcomeForbidden, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Bot access denied (403): operation={Operation} networkId={NetworkId} detail={Detail}",
                operation,
                id,
                detail);
        }

        try
        {
            await repository.AppendAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to append bot_access_log for {Operation}/{NetworkId}", operation, id);
        }
    }

    public async Task WriteFromExceptionAsync(
        string operation,
        string? networkId,
        string? botName,
        UseCaseException ex,
        CancellationToken cancellationToken = default)
    {
        await WriteAsync(operation, networkId, botName, ex.Code, ex.Message, cancellationToken)
            .ConfigureAwait(false);
    }
}
