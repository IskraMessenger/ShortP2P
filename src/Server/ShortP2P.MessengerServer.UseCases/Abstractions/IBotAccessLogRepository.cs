using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>Append-only bot access audit log (<c>bot_access_log</c>).</summary>
public interface IBotAccessLogRepository
{
    Task AppendAsync(BotAccessLogEntry entry, CancellationToken cancellationToken = default);
}
