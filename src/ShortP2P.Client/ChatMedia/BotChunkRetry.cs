namespace ShortP2P.Client.ChatMedia;

/// <summary>Retries one blob chunk. A failed index does not rewind chunks that already succeeded.</summary>
public static class BotChunkRetry
{
    public const int MaxAttempts = 5;

    public static async Task RunAsync(
        Func<CancellationToken, Task> action,
        Func<Exception, bool>? shouldRetry = null,
        Func<int, TimeSpan>? delayForAttempt = null,
        CancellationToken cancellationToken = default)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        shouldRetry ??= static ex => ex is not OperationCanceledException;
        delayForAttempt ??= static attempt => TimeSpan.FromMilliseconds(200 * (1 << (attempt - 1)));

        Exception? last = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await action(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
                if (!shouldRetry(ex) || attempt == MaxAttempts)
                    throw;
                var delay = delayForAttempt(attempt);
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        if (last != null)
            throw last;
    }

    public static async Task<T> RunAsync<T>(
        Func<CancellationToken, Task<T>> action,
        Func<Exception, bool>? shouldRetry = null,
        Func<int, TimeSpan>? delayForAttempt = null,
        CancellationToken cancellationToken = default)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        T? result = default;
        await RunAsync(
            async ct => { result = await action(ct).ConfigureAwait(false); },
            shouldRetry,
            delayForAttempt,
            cancellationToken).ConfigureAwait(false);
        return result!;
    }
}
