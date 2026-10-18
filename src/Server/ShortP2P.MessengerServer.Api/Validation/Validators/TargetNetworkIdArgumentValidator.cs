using FluentValidation;
using ShortP2P.MessengerServer.Api.Validation.Models;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class TargetNetworkIdArgumentValidator : AbstractValidator<TargetNetworkIdArgument>
{
    public TargetNetworkIdArgumentValidator()
    {
        RuleFor(x => x.TargetNetworkId).NetworkId("tgtNetworkId");
    }
}
