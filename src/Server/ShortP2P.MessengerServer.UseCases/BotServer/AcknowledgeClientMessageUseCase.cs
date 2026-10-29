using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.Blobs;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

/// <summary>
/// Bot delivery receipt: drops the stored client → bot message and every chunk stored under that message id.
/// A repeat receipt for an already removed message succeeds.
/// </summary>
public sealed class AcknowledgeClientMessageUseCase(
    IBotAccountRepository accounts,
    IClientToBotMessageRepository messages,
    IBlobChunkRepository chunks,
    IPasswordHasher passwordHasher,
    BotBlacklistGuard blacklistGuard)
{
    public async Task ExecuteAsync(
        AcknowledgeClientMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await blacklistGuard
            .EnsureAllowedAsync(command.BotNetworkId, BotAccessOperations.Receipts, cancellationToken)
            .ConfigureAwait(false);

        var account = await BotAuthentication.AuthenticateAsync(
            accounts,
            passwordHasher,
            command.BotNetworkId,
            command.BotKey,
            botName: null,
            requireBotName: false,
            cancellationToken).ConfigureAwait(false);

        var messageId = command.MessageId.Trim();
        var message = await messages.FindAsync(messageId, cancellationToken).ConfigureAwait(false);
        if (message != null &&
            !string.Equals(message.BotNetworkId, account.NetworkId, StringComparison.Ordinal))
        {
            throw UseCaseException.Unauthorized("Only the message recipient can submit a delivery receipt.");
        }

        if (message != null)
            await messages.RemoveAsync(messageId, cancellationToken).ConfigureAwait(false);

        await BlobChunkCleanup.DeleteForCallerAsync(chunks, messageId, account.NetworkId, cancellationToken)
            .ConfigureAwait(false);
    }
}
