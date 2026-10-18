using ShortP2P.MessengerServer.Api.Validation.Validators;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Tests.Bot;

public class DeliverBotMessagesInjectionTests
{
    [Fact]
    public async Task BotClientMessageDto_WhenClientNetworkIdUnsafe_IsInvalid()
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
}
