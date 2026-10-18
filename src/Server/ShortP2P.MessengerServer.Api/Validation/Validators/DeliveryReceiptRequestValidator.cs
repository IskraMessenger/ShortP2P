using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class DeliveryReceiptRequestValidator : AbstractValidator<DeliveryReceiptRequest>
{
    public DeliveryReceiptRequestValidator()
    {
        RuleFor(x => x.MessageId).MessageId();
        RuleFor(x => x.ReceivedAtUtc).RequiredUtcTimestamp("receivedAtUtc");
    }
}
