using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BotMessageReceiptRequestValidator : AbstractValidator<BotMessageReceiptRequest>
{
    public BotMessageReceiptRequestValidator()
    {
        RuleFor(x => x.MessageId).MessageId();
        RuleFor(x => x.ReceivedAtUtc).RequiredUtcTimestamp("receivedAtUtc");
    }
}
