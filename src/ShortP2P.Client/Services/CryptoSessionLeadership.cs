using ShortP2P.Auth.Data;

namespace ShortP2P.Client.Services;

/// <summary>
///     Crypto-session leadership: the peer with the smaller <see cref="CompressedNetworkId"/>
///     is the sole driver of RSA handshake initiation, <c>test_send</c> verification, and
///     renegotiation retries. The other peer only accepts invites / handshake and replies
///     with <c>test_response</c>.
/// </summary>
public static class CryptoSessionLeadership
{
    /// <summary>True when <paramref name="localNetworkIdShort"/> is the crypto-session leader.</summary>
    public static bool IsLeader(string localNetworkIdShort, string peerNetworkIdShort)
    {
        var ours = CompressedNetworkId.FromShortString(localNetworkIdShort.Trim());
        var peer = CompressedNetworkId.FromShortString(peerNetworkIdShort.Trim());
        return ours.CompareTo(peer) < 0;
    }

    /// <summary>Only the leader may start test_send / invite-driven renegotiation loops.</summary>
    public static bool ShouldDriveSessionVerification(bool isLeader) => isLeader;
}
