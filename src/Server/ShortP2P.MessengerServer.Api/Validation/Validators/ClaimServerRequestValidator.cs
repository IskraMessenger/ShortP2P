using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class ClaimServerRequestValidator : AbstractValidator<ClaimServerRequest>
{
    public ClaimServerRequestValidator()
    {
        RuleFor(x => x.ServerIp).RequiredTrimmed("serverIp is required.");
        RuleFor(x => x.ServerPort).InclusiveBetween(1, 65535).WithMessage("serverPort is invalid.");
        RuleFor(x => x.Reason).IsInEnum().WithMessage("reason is invalid.");
    }
}
