using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.Forwards;

public sealed class ForwardPeerProfileUseCase(
    IForwardHub forwardHub,
    IClientStatusRepository statuses,
    IInboxWaitService inboxWait,
    IClock clock,
    IOptions<MessengerInboxOptions> inboxOptions,
    ILogger<ForwardPeerProfileUseCase> logger)
{
    public Task ExecuteAsync(ForwardPeerProfileCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var forwardId = InputGuard.ForwardId(command.ForwardId);
        var src = InputGuard.NetworkId(command.SrcNetworkId, "srcNetworkId");
        var tgt = InputGuard.NetworkId(command.TgtNetworkId, "tgtNetworkId");
        var payloadB64 = InputGuard.CiphertextBase64(
            command.PayloadBase64, BlobLimits.MaxCiphertextBytes, "payloadBase64");
        var serverOrigin = string.IsNullOrWhiteSpace(command.ServerOrigin)
            ? "?"
            : CleartextFieldRules.Sanitize(command.ServerOrigin, 256);

        if (string.Equals(src, tgt, StringComparison.Ordinal))
            throw UseCaseException.Validation("srcNetworkId and tgtNetworkId must differ.");

        var payload = Convert.FromBase64String(payloadB64);
        var hashes = PeerProfileForwardPayloadInspector.ValidateAndHash(command.Kind, payload);

        logger.LogDebug(
            "Forward request body server={Server} forwardId={ForwardId} src={Src} tgt={Tgt} kind={Kind} payloadBytes={PayloadBytes}",
            serverOrigin, forwardId, src, tgt, command.Kind, payload.Length);

        return AcceptAsync(forwardId, src, tgt, command.Kind, payloadB64, hashes, serverOrigin, cancellationToken);
    }

    private async Task AcceptAsync(
        string forwardId,
        string src,
        string tgt,
        ForwardKind kind,
        string payloadB64,
        PeerProfileForwardPayloadInspector.InspectResult hashes,
        string serverOrigin,
        CancellationToken cancellationToken)
    {
        if (!await IsTargetOnlineAsync(tgt, cancellationToken).ConfigureAwait(false))
            throw UseCaseException.PeerOffline("Target peer is offline; forward was not queued.");

        var now = clock.UtcNow;
        forwardHub.PurgeExpired(now);

        var envelope = new ForwardEnvelope(forwardId, src, tgt, kind, payloadB64, now);
        if (!forwardHub.TryAdd(envelope))
        {
            logger.LogInformation(
                "Forward duplicate ignored src={Src} tgt={Tgt} forwardId={ForwardId} server={Server} avatarHash={AvatarHash} aboutHash={AboutHash}",
                src, tgt, forwardId, serverOrigin, hashes.AvatarHashHex, hashes.AboutHashHex);
            return;
        }

        logger.LogInformation(
            "Forward accepted src={Src} tgt={Tgt} forwardId={ForwardId} server={Server} avatarHash={AvatarHash} aboutHash={AboutHash}",
            src, tgt, forwardId, serverOrigin, hashes.AvatarHashHex, hashes.AboutHashHex);

        inboxWait.Notify(tgt);
    }

    private async Task<bool> IsTargetOnlineAsync(string tgtNetworkId, CancellationToken cancellationToken)
    {
        var onlineTimeout = inboxOptions.Value.OnlineTimeout;
        var now = clock.UtcNow;
        var all = await statuses.ListAllAsync(cancellationToken).ConfigureAwait(false);
        return all.Any(s =>
            string.Equals(s.NetworkId, tgtNetworkId, StringComparison.Ordinal) &&
            s.Status == ClientOnlineStatus.Online &&
            now - s.CreatedAtUtc <= onlineTimeout);
    }
}
