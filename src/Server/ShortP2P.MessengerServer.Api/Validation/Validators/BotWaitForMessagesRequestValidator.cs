using FluentValidation;
using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BotWaitForMessagesRequestValidator : AbstractValidator<BotWaitForMessagesRequest>
{
    public BotWaitForMessagesRequestValidator(IOptions<MessengerInboxOptions> inboxOptions)
    {
        var max = inboxOptions.Value.MaxPollTimeoutSeconds;
        RuleFor(x => x.NetworkId).NetworkId();
        RuleFor(x => x.TimeoutSeconds).OptionalTimeoutSeconds(max);
    }
}
