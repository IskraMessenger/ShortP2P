using FluentValidation;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BotSendMessageRequestValidator : AbstractValidator<BotSendMessageRequest>
{
    public BotSendMessageRequestValidator()
    {
        RuleFor(x => x.MessageId).MessageId();
        RuleFor(x => x.NetworkId).NetworkId("botNetworkId");
        RuleFor(x => x.DateTime).RequiredUtcTimestamp("dateTime");
        RuleFor(x => x.Message).CiphertextBase64(MessageLimits.MaxCiphertextBytes, "message");
        RuleFor(x => x.CorrelationId).OptionalCorrelationId();
    }
}
