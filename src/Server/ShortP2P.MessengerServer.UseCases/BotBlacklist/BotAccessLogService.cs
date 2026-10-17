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
        var id = CleartextFieldRules.Sanitize(networkId, NetworkIdRules.MaxLength);
        var safeBotName = string.IsNullOrWhiteSpace(botName)
            ? null
            : CleartextFieldRules.Sanitize(botName, 256);
        if (string.IsNullOrEmpty(safeBotName))
            safeBotName = null;

        var entry = new BotAccessLogEntry
        {
            Id = 0,
            AtUtc = clock.UtcNow,
            NetworkId = id,
            BotName = safeBotName,
            Operation = CleartextFieldRules.Sanitize(operation, 64),
            Outcome = CleartextFieldRules.Sanitize(outcome, 64),
            Detail = CleartextFieldRules.Sanitize(detail, 1024)
        };

        if (string.Equals(entry.Outcome, OutcomeForbidden, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Bot access denied (403): operation={Operation} networkId={NetworkId} detail={Detail}",
                entry.Operation,
                entry.NetworkId,
                entry.Detail);
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
