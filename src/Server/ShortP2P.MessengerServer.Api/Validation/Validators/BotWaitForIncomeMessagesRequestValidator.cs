using FluentValidation;
using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BotWaitForIncomeMessagesRequestValidator : AbstractValidator<BotWaitForIncomeMessagesRequest>
{
    public BotWaitForIncomeMessagesRequestValidator(IOptions<MessengerInboxOptions> inboxOptions)
    {
        var max = inboxOptions.Value.MaxPollTimeoutSeconds;
        RuleFor(x => x.RequestId).RequiredTrimmed("requestId is required.");
        RuleFor(x => x.BotNetworkId).NetworkId("botNetworkId");
        RuleFor(x => x.BotKey).BotKey();
        RuleFor(x => x.TimeoutSeconds).OptionalTimeoutSeconds(max);
    }
}
