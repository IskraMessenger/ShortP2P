using ShortP2P.MessengerServer.Tests.Fakes;
using ShortP2P.MessengerServer.UseCases;
using ShortP2P.MessengerServer.UseCases.Auth;

namespace ShortP2P.MessengerServer.Tests.Auth;

public class RegisterClientInjectionTests
{
    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("evil\nnick")]
    [InlineData("tag>break")]
    public async Task ExecuteAsync_WhenNickUnsafe_ThrowsValidation(string nick)
    {
        var h = new TestHarness();
        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            h.RegisterClient().ExecuteAsync(
                new RegisterClientCommand(nick, "net-a", "secret", TestIds.DeviceA)));
        Assert.Equal("Validation", ex.Code);
        Assert.DoesNotContain(nick, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNetworkIdHasSqlChars_ThrowsValidation()
    {
        var h = new TestHarness();
        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            h.RegisterClient().ExecuteAsync(
                new RegisterClientCommand("Alice", "net'or'1", "secret", TestIds.DeviceA)));
        Assert.Equal("Validation", ex.Code);
    }
}
