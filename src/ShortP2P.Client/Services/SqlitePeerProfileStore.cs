using ShortP2P.Auth.Data;
using ShortP2P.Client.ChatMedia;
using ShortP2P.Client.Data;
using ShortP2P.Discovery.Profile;

namespace ShortP2P.Client.Services;

public sealed class SqlitePeerProfileStore(AppDatabase appDatabase) : IPeerProfileStore
{
    private readonly AppDatabase _db = appDatabase ?? throw new ArgumentNullException(nameof(appDatabase));

    public event EventHandler<PeerProfileChangedEventArgs>? Changed;

    public async ValueTask UpsertAsync(CompressedNetworkId networkId, string? nickname, string aboutMe,
        byte[]? avatar, CancellationToken cancellationToken = default)
    {
        if (networkId.IsEmpty)
            return;

        aboutMe ??= "";
        if (aboutMe.Length > PeerProfileLimits.MaxAboutMeChars)
            aboutMe = aboutMe[..PeerProfileLimits.MaxAboutMeChars];

        var applyAvatar = true;
        if (avatar is { Length: 0 })
            avatar = null;
        else if (avatar != null && avatar.Length > PeerProfileLimits.MaxAvatarBytes)
        {
            if (avatar.Length <= PeerProfileLimits.MaxAvatarDisplayBytes &&
                ImageAttachmentCompressor.TryCompressToMaxBytes(avatar, PeerProfileLimits.MaxAvatarBytes,
                    out var shrunk, out _))
            {
                avatar = shrunk;
            }
            else
            {
                // Do not wipe a good stored avatar when an oversized inbound blob cannot be shrunk.
                applyAvatar = false;
                avatar = null;
            }
        }

        var idShort = networkId.ToShortString();
        var nick = nickname is null || string.IsNullOrWhiteSpace(nickname) ? "" : nickname.Trim();

        await _db.WriteAsync(async conn =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = DateTime.UtcNow.Ticks;
            var row = await conn.FindAsync<PeerProfileEntity>(idShort).ConfigureAwait(false);
            if (row == null)
            {
                await conn.InsertAsync(new PeerProfileEntity
                {
                    NetworkIdShort = idShort,
                    Nickname = nick,
                    AboutMe = aboutMe,
                    Avatar = applyAvatar ? avatar : null,
                    UpdatedUtcTicks = now
                }).ConfigureAwait(false);
            }
            else
            {
                if (nick.Length > 0)
                    row.Nickname = nick;
                row.AboutMe = aboutMe;
                if (applyAvatar)
                    row.Avatar = avatar;
                row.UpdatedUtcTicks = now;
                await conn.UpdateAsync(row).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);

        try
        {
            Changed?.Invoke(this, new PeerProfileChangedEventArgs(networkId));
        }
        catch
        {
            // UI listeners must not break profile persistence.
        }
    }

    public async ValueTask<PeerProfileSnapshot?> GetAsync(CompressedNetworkId networkId,
        CancellationToken cancellationToken = default)
    {
        if (networkId.IsEmpty)
            return null;

        var idShort = networkId.ToShortString();
        PeerProfileSnapshot? snap = await _db.ReadAsync(async conn =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = await conn.FindAsync<PeerProfileEntity>(idShort).ConfigureAwait(false);
            if (row == null)
                return null;
            return new PeerProfileSnapshot
            {
                NetworkId = networkId,
                Nickname = row.Nickname ?? "",
                AboutMe = row.AboutMe ?? "",
                Avatar = row.Avatar,
                UpdatedUtc = new DateTimeOffset(row.UpdatedUtcTicks, TimeSpan.Zero)
            };
        }).ConfigureAwait(false);

        if (snap?.Avatar is not { Length: > PeerProfileLimits.MaxAvatarBytes } blob)
            return snap;
        if (blob.Length > PeerProfileLimits.MaxAvatarDisplayBytes)
        {
            // Unusable blob — hide from UI but keep row.
            return new PeerProfileSnapshot
            {
                NetworkId = snap.NetworkId,
                Nickname = snap.Nickname,
                AboutMe = snap.AboutMe,
                Avatar = null,
                UpdatedUtc = snap.UpdatedUtc
            };
        }

        if (!ImageAttachmentCompressor.TryCompressToMaxBytes(blob, PeerProfileLimits.MaxAvatarBytes,
                out var shrunk, out _) || shrunk == null)
        {
            // Still show legacy blob so restart does not fall back to initials.
            return snap;
        }

        try
        {
            await _db.WriteAsync(async conn =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = await conn.FindAsync<PeerProfileEntity>(idShort).ConfigureAwait(false);
                if (row == null)
                    return;
                row.Avatar = shrunk;
                row.UpdatedUtcTicks = DateTime.UtcNow.Ticks;
                await conn.UpdateAsync(row).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort shrink; still return shrunk for this read.
        }

        return new PeerProfileSnapshot
        {
            NetworkId = snap.NetworkId,
            Nickname = snap.Nickname,
            AboutMe = snap.AboutMe,
            Avatar = shrunk,
            UpdatedUtc = snap.UpdatedUtc
        };
    }
}
