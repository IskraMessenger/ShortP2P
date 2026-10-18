using ShortP2P.MessengerServer.Api.Validation.Validators;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.Tests.Fakes;

namespace ShortP2P.MessengerServer.Tests.Auth;

public class RegisterClientInjectionTests
{
    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("evil\nnick")]
    [InlineData("tag>break")]
    public async Task RegisterRequest_WhenNickUnsafe_IsInvalid(string nick)
    {
        var result = await new RegisterRequestValidator().ValidateAsync(new RegisterRequest
        {
            Nick = nick,
            NetworkId = "net-a",
            Password = "secret",
            DeviceId = TestIds.DeviceA
        });
        Assert.False(result.IsValid);
        Assert.DoesNotContain(nick, result.Errors[0].ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterRequest_WhenNetworkIdHasSqlChars_IsInvalid()
    {
        var result = await new RegisterRequestValidator().ValidateAsync(new RegisterRequest
        {
            Nick = "Alice",
            NetworkId = "net'or'1",
            Password = "secret",
            DeviceId = TestIds.DeviceA
        });
        Assert.False(result.IsValid);
    }
}
