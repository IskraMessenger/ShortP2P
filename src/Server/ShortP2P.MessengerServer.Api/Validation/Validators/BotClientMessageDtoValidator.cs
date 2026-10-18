using FluentValidation;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BotClientMessageDtoValidator : AbstractValidator<BotClientMessageDto>
{
    public BotClientMessageDtoValidator()
    {
        RuleFor(x => x.NetworkId).NetworkId("clientNetworkId");
        RuleFor(x => x.EncryptedMessageBase64)
            .CiphertextBase64(MessageLimits.MaxCiphertextBytes, "encryptedMessage");
        RuleFor(x => x.CorrelationId).OptionalCorrelationId();
    }
}
