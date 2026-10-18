using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Api.Validation.Models;
using ShortP2P.MessengerServer.Api.Validation.Validators;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.Tests.Fakes;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Tests.Validation;

public class RequestValidatorTests
{
    private static readonly IOptions<MessengerInboxOptions> Inbox =
        Options.Create(new MessengerInboxOptions { MaxPollTimeoutSeconds = 30 });

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

    [Theory]
    [InlineData("", "net", "pw", TestIds.DeviceA)]
    [InlineData("nick", "", "pw", TestIds.DeviceA)]
    [InlineData("nick", "net", "", TestIds.DeviceA)]
    [InlineData("nick", "net", "pw", "short")]
    public async Task RegisterRequest_WhenInvalidInput_IsInvalid(
        string nick, string networkId, string password, string deviceId)
    {
        var result = await new RegisterRequestValidator().ValidateAsync(new RegisterRequest
        {
            Nick = nick,
            NetworkId = networkId,
            Password = password,
            DeviceId = deviceId
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task LoginRequest_WhenDeviceIdInvalid_IsInvalid()
    {
        var result = await new LoginRequestValidator().ValidateAsync(new LoginRequest
        {
            NetworkId = "net-a",
            Password = "secret",
            DeviceId = "bad"
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task BotRegisterRequest_WhenFingerprintInvalid_IsInvalid()
    {
        var result = await new BotRegisterRequestValidator().ValidateAsync(new BotRegisterRequest
        {
            NetworkId = "bot-net",
            BotName = "echo",
            BotReadableName = "Echo",
            BotDescription = "desc",
            FingerprintSha256 = "short"
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task BotClientMessageDto_WhenNetworkIdUnsafe_IsInvalid()
    {
        var result = await new BotClientMessageDtoValidator().ValidateAsync(new BotClientMessageDto
        {
            NetworkId = "client';drop",
            EncryptedMessageBase64 = "Y2lwaGVy"
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task BotClientMessageDto_WhenCiphertextNotBase64_IsInvalid()
    {
        var result = await new BotClientMessageDtoValidator().ValidateAsync(new BotClientMessageDto
        {
            NetworkId = "client",
            EncryptedMessageBase64 = "not!!!base64"
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task BotSendMessageRequest_WhenCorrelationTooLong_IsInvalid()
    {
        var result = await new BotSendMessageRequestValidator().ValidateAsync(new BotSendMessageRequest
        {
            MessageId = "mid",
            NetworkId = "bot",
            DateTime = DateTime.UtcNow,
            Message = "Y2lwaGVy",
            CorrelationId = new string('x', 33)
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task BotSendMessageRequest_WhenCiphertextNotBase64_IsInvalid()
    {
        var result = await new BotSendMessageRequestValidator().ValidateAsync(new BotSendMessageRequest
        {
            MessageId = "mid",
            NetworkId = "bot",
            DateTime = DateTime.UtcNow,
            Message = "not-valid-base64!!!"
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task BotSendMessageRequest_WhenCallerNetworkIdUnsafe_IsInvalid()
    {
        // NetworkId on this DTO is the bot destination; injection on caller is JWT-side.
        // Cover bot network id alphabet via the same rule.
        var result = await new BotSendMessageRequestValidator().ValidateAsync(new BotSendMessageRequest
        {
            MessageId = "mid",
            NetworkId = "bot';drop",
            DateTime = DateTime.UtcNow,
            Message = "Y2lwaGVy"
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task MessageDto_WhenMessageIdEmpty_IsInvalid()
    {
        var result = await new MessageDtoValidator().ValidateAsync(new MessageDto
        {
            MessageId = "",
            SrcNetworkId = "a",
            TgtNetworkId = "b",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
            EncryptedDataBase64 = Convert.ToBase64String([1, 2, 3])
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ForwardRequest_WhenInvalidBase64_IsInvalid()
    {
        var result = await new ForwardRequestValidator().ValidateAsync(new ForwardRequest
        {
            ForwardId = "fwd-1",
            SrcNetworkId = "alice",
            TgtNetworkId = "bob",
            Kind = ForwardKind.PeerProfileRequest,
            PayloadBase64 = "%%%"
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task EventsPollArguments_WhenTimeoutOutOfRange_IsInvalid()
    {
        var result = await new EventsPollArgumentsValidator(Inbox).ValidateAsync(
            new EventsPollArguments(TestIds.DeviceB, 999));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task EventsPollArguments_WhenDeviceIdInvalid_IsInvalid()
    {
        var result = await new EventsPollArgumentsValidator(Inbox).ValidateAsync(
            new EventsPollArguments("bad", 1));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ChatRequestCreateRequest_WhenPublicKeyMissing_IsInvalid()
    {
        var result = await new ChatRequestCreateRequestValidator().ValidateAsync(
            new ChatRequestCreateRequest { PublicKey = "", TargetNetworkId = "bob" });
        Assert.False(result.IsValid);
    }
}
