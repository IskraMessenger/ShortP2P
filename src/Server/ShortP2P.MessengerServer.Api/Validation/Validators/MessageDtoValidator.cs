using FluentValidation;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class MessageDtoValidator : AbstractValidator<MessageDto>
{
    public MessageDtoValidator()
    {
        RuleFor(x => x.MessageId).MessageId();
        RuleFor(x => x.SrcNetworkId).NetworkId("srcNetworkId");
        RuleFor(x => x.TgtNetworkId).NetworkId("tgtNetworkId");
        RuleFor(x => x.EncryptedDataBase64)
            .CiphertextBase64(MessageLimits.MaxCiphertextBytes, "encryptedDataBase64");
    }
}
