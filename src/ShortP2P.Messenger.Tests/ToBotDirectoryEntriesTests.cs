using ShortP2P.Client.Services.MessengerServers;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.Messenger.Tests;

public class ToBotDirectoryEntriesTests
{
    [Fact]
    public void MapsCatalogPublicKeyAndBotFlag()
    {
        var entries = MessengerServerSyncService.ToBotDirectoryEntries(
        [
            new BotCatalogDto
            {
                NetworkId = " bot-net ",
                BotName = "echo",
                BotReadableName = " Echo Bot ",
                BotDescription = " desc ",
                MessagePublicKey = " {\"k\":\"bot-pub\"} "
            }
        ]);

        var entry = Assert.Single(entries);
        Assert.Equal("bot-net", entry.NetworkIdShort);
        Assert.Equal("Echo Bot", entry.Nickname);
        Assert.True(entry.IsBot);
        Assert.Equal("desc", entry.AboutMe);
        Assert.Equal("{\"k\":\"bot-pub\"}", entry.MessagePublicKey);
    }
}
