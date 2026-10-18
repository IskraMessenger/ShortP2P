using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BotSendMessagesRequestValidator : AbstractValidator<BotSendMessagesRequest>
{
    public BotSendMessagesRequestValidator(IValidator<BotClientMessageDto> messageValidator)
    {
        RuleFor(x => x.RequestId).RequiredTrimmed("requestId is required.");
        RuleFor(x => x.BotNetworkId).NetworkId("botNetworkId");
        RuleFor(x => x.BotKey).BotKey();
        RuleFor(x => x.Messages)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Messages are required.")
            .Must(m => m.Count > 0)
            .WithMessage("Messages are required.");
        RuleForEach(x => x.Messages).SetValidator(messageValidator);
    }
}
