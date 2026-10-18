using FluentValidation;
using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Api.Validation.Models;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class EventsPollArgumentsValidator : AbstractValidator<EventsPollArguments>
{
    public EventsPollArgumentsValidator(IOptions<MessengerInboxOptions> inboxOptions)
    {
        var max = inboxOptions.Value.MaxPollTimeoutSeconds;
        RuleFor(x => x.DeviceId).DeviceId();
        RuleFor(x => x.TimeoutSeconds).OptionalTimeoutSeconds(max);
    }
}
