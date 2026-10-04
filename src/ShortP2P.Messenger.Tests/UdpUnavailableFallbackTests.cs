using ShortP2P.Client.Services;
using ShortP2P.Client.Services.MessengerServers;

namespace ShortP2P.Messenger.Tests;

public class UdpUnavailableFallbackTests
{
    [Fact]
    public void ShouldTryBleFallback_OnlyWhenEnabledAvailableAndPeerHasMac()
    {
        Assert.True(ChatP2PSession.ShouldTryBleFallback(true, true, true));
        Assert.False(ChatP2PSession.ShouldTryBleFallback(false, true, true));
        Assert.False(ChatP2PSession.ShouldTryBleFallback(true, false, true));
        Assert.False(ChatP2PSession.ShouldTryBleFallback(true, true, false));
    }

    [Fact]
    public void SelectHostingServerId_PrefersOnlineSubscriberThenFirstPresent()
    {
        var ranked = new MessengerServerSyncService.HostingServerCandidate[]
        {
            new(ServerId: 1, SubscriberPresent: false, SubscriberOnline: false),
            new(ServerId: 2, SubscriberPresent: true, SubscriberOnline: false),
            new(ServerId: 3, SubscriberPresent: true, SubscriberOnline: true)
        };

        Assert.Equal(3, MessengerServerSyncService.SelectHostingServerId(ranked));

        var offlineOnly = new MessengerServerSyncService.HostingServerCandidate[]
        {
            new(ServerId: 4, SubscriberPresent: false, SubscriberOnline: true),
            new(ServerId: 5, SubscriberPresent: true, SubscriberOnline: false),
            new(ServerId: 6, SubscriberPresent: true, SubscriberOnline: false)
        };

        Assert.Equal(5, MessengerServerSyncService.SelectHostingServerId(offlineOnly));
        Assert.Null(MessengerServerSyncService.SelectHostingServerId(
            [new(ServerId: 9, SubscriberPresent: false, SubscriberOnline: false)]));
    }

    [Fact]
    public void ManualPath_OverridesAutomaticServerFallbackUntilSwitched()
    {
        Assert.True(ChatP2PSession.AllowsAutomaticServerFallback(ChatDeliveryPath.Auto));
        Assert.False(ChatP2PSession.AllowsAutomaticServerFallback(ChatDeliveryPath.Server));
        Assert.False(ChatP2PSession.AllowsAutomaticServerFallback(ChatDeliveryPath.Mesh));

        Assert.Equal(ChatDeliveryPath.Server, ChatP2PSession.NextManualDeliveryPath(ChatDeliveryPath.Auto));
        Assert.Equal(ChatDeliveryPath.Mesh, ChatP2PSession.NextManualDeliveryPath(ChatDeliveryPath.Server));
        Assert.Equal(ChatDeliveryPath.Server, ChatP2PSession.NextManualDeliveryPath(ChatDeliveryPath.Mesh));
    }
}
