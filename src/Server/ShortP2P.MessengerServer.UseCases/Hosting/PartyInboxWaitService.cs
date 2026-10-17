using System.Collections.Concurrent;

namespace ShortP2P.MessengerServer.UseCases.Hosting;

/// <summary>Single-process wait registry. Several polls for the same party stay registered.</summary>
public abstract class PartyInboxWaitService
{
    private readonly ConcurrentDictionary<string, WaitGroup> _waiters = new(StringComparer.Ordinal);

    protected async Task WaitCoreAsync(
        string partyId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var key = partyId.Trim();
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var group = _waiters.GetOrAdd(key, _ => new WaitGroup());
        lock (group.Sync)
            group.Waiters.Add(tcs);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);
            await tcs.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // timeout — normal empty poll
        }
        finally
        {
            lock (group.Sync)
                group.Waiters.Remove(tcs);
        }
    }

    protected void NotifyCore(string partyId)
    {
        var key = partyId.Trim();
        if (!_waiters.TryGetValue(key, out var group))
            return;

        List<TaskCompletionSource> snapshot;
        lock (group.Sync)
            snapshot = [.. group.Waiters];

        foreach (var tcs in snapshot)
            tcs.TrySetResult();
    }

    private sealed class WaitGroup
    {
        public readonly Lock Sync = new();
        public readonly List<TaskCompletionSource> Waiters = [];
    }
}
