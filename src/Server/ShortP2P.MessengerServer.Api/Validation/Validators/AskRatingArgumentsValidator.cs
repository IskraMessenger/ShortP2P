using FluentValidation;
using ShortP2P.MessengerServer.Api.Validation.Models;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class AskRatingArgumentsValidator : AbstractValidator<AskRatingArguments>
{
    public AskRatingArgumentsValidator()
    {
        RuleFor(x => x.ServerIp).RequiredTrimmed("serverIp is required.");
        RuleFor(x => x.ServerPort).InclusiveBetween(1, 65535).WithMessage("serverPort is invalid.");
    }
}
