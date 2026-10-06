using ShortP2P.Auth.Data;
using ShortP2P.Client.Services;

namespace ShortP2P.Messenger.Tests;

public class CryptoSessionLeadershipTests
{
    [Fact]
    public void IsLeader_SmallerNetworkId_IsLeader()
    {
        var a = CompressedNetworkId.FromWireBytes(MakeWire(1));
        var b = CompressedNetworkId.FromWireBytes(MakeWire(2));
        Assert.True(a.CompareTo(b) < 0);

        Assert.True(CryptoSessionLeadership.IsLeader(a.ToShortString(), b.ToShortString()));
        Assert.False(CryptoSessionLeadership.IsLeader(b.ToShortString(), a.ToShortString()));
    }

    [Fact]
    public void IsLeader_EqualIds_IsNotLeader()
    {
        var id = CompressedNetworkId.FromWireBytes(MakeWire(7));
        var s = id.ToShortString();
        Assert.False(CryptoSessionLeadership.IsLeader(s, s));
    }

    [Fact]
    public void ShouldDriveSessionVerification_OnlyLeader()
    {
        Assert.True(CryptoSessionLeadership.ShouldDriveSessionVerification(isLeader: true));
        Assert.False(CryptoSessionLeadership.ShouldDriveSessionVerification(isLeader: false));
    }

    [Fact]
    public void Leadership_IsDeterministic_AcrossPeers()
    {
        var left = CompressedNetworkId.New();
        var right = CompressedNetworkId.New();
        var leftShort = left.ToShortString();
        var rightShort = right.ToShortString();

        var leftLeads = CryptoSessionLeadership.IsLeader(leftShort, rightShort);
        var rightLeads = CryptoSessionLeadership.IsLeader(rightShort, leftShort);

        // Exactly one peer must drive session verification.
        Assert.NotEqual(leftLeads, rightLeads);
        Assert.Equal(leftLeads, CryptoSessionLeadership.ShouldDriveSessionVerification(leftLeads));
        Assert.Equal(rightLeads, CryptoSessionLeadership.ShouldDriveSessionVerification(rightLeads));
    }

    private static byte[] MakeWire(byte last)
    {
        var bytes = new byte[CompressedNetworkId.WireLength];
        bytes[^1] = last;
        return bytes;
    }
}
