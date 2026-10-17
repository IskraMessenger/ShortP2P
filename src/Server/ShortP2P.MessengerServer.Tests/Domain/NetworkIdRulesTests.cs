using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Tests.Domain;

public class NetworkIdRulesTests
{
    [Theory]
    [InlineData("alice", true)]
    [InlineData("bot_1-A", true)]
    [InlineData("client';drop", false)]
    [InlineData("a b", false)]
    [InlineData("<script>", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_ChecksBase64UrlAlphabet(string? value, bool expected) =>
        Assert.Equal(expected, NetworkIdRules.IsValid(value));
}
