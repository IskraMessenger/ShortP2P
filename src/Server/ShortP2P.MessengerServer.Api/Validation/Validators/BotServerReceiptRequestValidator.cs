using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BotServerReceiptRequestValidator : AbstractValidator<BotServerReceiptRequest>
{
    public BotServerReceiptRequestValidator()
    {
        RuleFor(x => x.RequestId).RequiredTrimmed("requestId is required.");
        RuleFor(x => x.BotNetworkId).NetworkId("botNetworkId");
        RuleFor(x => x.BotKey).BotKey();
        RuleFor(x => x.MessageId).MessageId();
    }
}
