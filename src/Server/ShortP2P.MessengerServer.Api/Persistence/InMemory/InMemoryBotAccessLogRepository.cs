using System.Collections.Concurrent;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Api.Persistence.InMemory;

public sealed class InMemoryBotAccessLogRepository : IBotAccessLogRepository
{
    private readonly ConcurrentQueue<BotAccessLogEntry> _entries = new();
    private long _nextId;

    public Task AppendAsync(BotAccessLogEntry entry, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = Interlocked.Increment(ref _nextId);
        _entries.Enqueue(new BotAccessLogEntry
        {
            Id = id,
            AtUtc = entry.AtUtc,
            NetworkId = entry.NetworkId,
            BotName = entry.BotName,
            Operation = entry.Operation,
            Outcome = entry.Outcome,
            Detail = entry.Detail
        });
        return Task.CompletedTask;
    }

    /// <summary>Test/diagnostic snapshot of appended rows.</summary>
    public IReadOnlyList<BotAccessLogEntry> Snapshot() => _entries.ToArray();
}
