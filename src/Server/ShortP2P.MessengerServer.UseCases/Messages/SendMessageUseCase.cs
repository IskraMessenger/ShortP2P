using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.Inbox;

namespace ShortP2P.MessengerServer.UseCases.Messages;

public sealed class SendMessageUseCase(
    IMessageRepository messages,
    IMessageCache messageCache,
    MessengerCacheOptions options,
    DeviceFanoutService fanout,
    IInboxWaitService inboxWait)
{
    public async Task ExecuteAsync(SendMessageCommand command, CancellationToken cancellationToken = default)
    {
        var raw = command.Message ?? throw UseCaseException.Validation("Message is required.");

        var message = new Message
        {
            MessageId = raw.MessageId.Trim(),
            SrcNetworkId = raw.SrcNetworkId.Trim(),
            TgtNetworkId = raw.TgtNetworkId.Trim(),
            CreatedUtc = raw.CreatedUtc,
            UpdatedUtc = raw.UpdatedUtc,
            EncryptedDataBase64 = raw.EncryptedDataBase64.Trim()
        };

        StorageAccess.EnsureAnyStoreEnabled(options);

        if (options.CacheEnabled)
        {
            var existingInCache = await StorageAccess
                .TryGetAsync(() => messageCache.FindByIdAsync(message.MessageId, cancellationToken), cancellationToken)
                .ConfigureAwait(false);
            if (existingInCache is not null)
                return;
        }

        if (options.RepositoryEnabled)
        {
            var existingInRepo = await StorageAccess
                .TryGetAsync(() => messages.FindByIdAsync(message.MessageId, cancellationToken), cancellationToken)
                .ConfigureAwait(false);
            if (existingInRepo is not null)
                return;
        }

        var writes = new List<Task<bool>>(2);
        if (options.CacheEnabled)
        {
            writes.Add(StorageAccess.TryWriteToCacheAsync(
                cacheEnabled: true,
                () => messageCache.IsWriteAvailable,
                () => messageCache.AddAsync(message, cancellationToken),
                cancellationToken));
        }

        if (options.RepositoryEnabled)
            writes.Add(StorageAccess.TryWriteAsync(() => messages.AddAsync(message, cancellationToken), cancellationToken));

        var results = await Task.WhenAll(writes).ConfigureAwait(false);
        if (!results.Any(ok => ok))
            throw UseCaseException.Unavailable("Failed to store message: cache and repository are unavailable.");

        await fanout.FanOutMessageAsync(message, cancellationToken).ConfigureAwait(false);
        inboxWait.Notify(message.TgtNetworkId);
    }
}
